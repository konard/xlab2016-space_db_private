using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Magic.Kernel.Compilation;
using Magic.Kernel.Core;
using Magic.Kernel.Interpretation;
using Magic.Kernel.Processor;
using System.IdentityModel.Tokens.Jwt;

namespace Magic.Kernel.Devices.Streams
{
    /// <summary>
    /// Claw device: an HTTP socket server that listens on a configurable port.
    /// Protocol: receives JSON, returns JSON.
    /// POST /authenticate {user,password} => {token} with progressive lockout.
    /// POST /entrypoint (Bearer auth) {method, data:{command,...}} dispatches to registered AGI procedures.
    /// Bind AGI procedures with: claw1.methods.add("methodName", "&amp;procedureName").
    /// Inside the bound procedure: var socket1 := socket; provides access to connection context.
    /// </summary>
    public class ClawStreamDevice : DefStream
    {
        private int _port;
        private List<Credential> _credentials = new List<Credential>();
        private readonly ConcurrentDictionary<string, string> _tokens = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, UserLockout> _lockouts = new ConcurrentDictionary<string, UserLockout>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _methodMap = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // JWT config (auth server-mode device)
        // Use authentication.jwtSecret in claw.open config to override in dev/prod.
        private string _jwtSecret = "dev_claw_jwt_secret_change_me";
        private TimeSpan _jwtTtl = TimeSpan.FromMinutes(30);
        private readonly JwtSecurityTokenHandler _jwtTokenHandler = new JwtSecurityTokenHandler();

        private HttpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _serverTask;

        private KernelConfiguration? _kernelConfig;
        private ExecutableUnit? _unit;

        /// <summary>Methods registry: methodName => AGI procedure name.</summary>
        public ClawMethodsRegistry Methods { get; }

        /// <summary>Current configured/listening port.</summary>
        public int Port => _port;

        /// <summary>Execution unit name that created this stream (if available).</summary>
        public string UnitName => _unit?.Name ?? string.Empty;

        /// <summary>Execution unit instance index that created this stream (if available).</summary>
        public int? UnitInstanceIndex => _unit?.InstanceIndex;

        /// <summary>True when HTTP listener is active and background server loop is running.</summary>
        public bool IsListening => _listener?.IsListening == true && _serverTask != null && !_serverTask.IsCompleted;

        private string LogPrefix
        {
            get
            {
                var serverName = string.IsNullOrWhiteSpace(Name) ? "claw" : Name.Trim();
                return $"[{serverName}] [claw]";
            }
        }

        public ClawStreamDevice()
        {
            Methods = new ClawMethodsRegistry(_methodMap);
        }

        public override async Task<object?> CallObjAsync(string methodName, object?[] args)
        {
            var name = methodName?.Trim() ?? "";

            if (string.Equals(name, "open", StringComparison.OrdinalIgnoreCase))
                return await HandleOpenAsync(args).ConfigureAwait(false);

            if (string.Equals(name, "methods", StringComparison.OrdinalIgnoreCase))
                return Methods;

            throw new CallUnknownMethodException(name, this);
        }

        private async Task<DeviceOperationResult> HandleOpenAsync(object?[]? args)
        {
            if (args != null && args.Length > 0)
                ParseConfig(args[0]);

            // Capture kernel config and unit from execution context
            _kernelConfig = ExecutionCallContext?.Interpreter?.Configuration;
            _unit = ExecutionCallContext?.Unit;

            return await OpenAsync().ConfigureAwait(false);
        }

        private void ParseConfig(object? config)
        {
            if (config == null) return;

            if (config is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("port", out var portObj))
                    _port = ParseInt(portObj, 8080);

                if (dict.TryGetValue("authentication", out var authObj) && authObj is Dictionary<string, object> authDict)
                {
                    if (authDict.TryGetValue("credentials", out var credsObj))
                        _credentials = ParseCredentials(credsObj);

                    if (authDict.TryGetValue("jwtSecret", out var jwtSecretObj) && jwtSecretObj != null)
                    {
                        var s = jwtSecretObj.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                            _jwtSecret = s;
                    }

                    if (authDict.TryGetValue("jwtTtlSeconds", out var jwtTtlSecondsObj) && jwtTtlSecondsObj != null)
                    {
                        var seconds = ParseInt(jwtTtlSecondsObj, (int)_jwtTtl.TotalSeconds);
                        if (seconds > 0)
                            _jwtTtl = TimeSpan.FromSeconds(seconds);
                    }
                }
            }
            else if (config is string jsonStr)
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonStr);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("port", out var portEl))
                        _port = portEl.ValueKind == JsonValueKind.Number ? portEl.GetInt32() : 8080;
                    if (root.TryGetProperty("authentication", out var authEl))
                    {
                        if (authEl.TryGetProperty("credentials", out var credsEl))
                            _credentials = ParseCredentialsFromJson(credsEl);

                        if (authEl.TryGetProperty("jwtSecret", out var jwtSecretEl))
                        {
                            var s = jwtSecretEl.ValueKind == JsonValueKind.String ? jwtSecretEl.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(s))
                                _jwtSecret = s;
                        }

                        if (authEl.TryGetProperty("jwtTtlSeconds", out var jwtTtlSecondsEl) &&
                            jwtTtlSecondsEl.ValueKind == JsonValueKind.Number &&
                            jwtTtlSecondsEl.TryGetInt32(out var seconds) &&
                            seconds > 0)
                        {
                            _jwtTtl = TimeSpan.FromSeconds(seconds);
                        }
                    }
                }
                catch { }
            }

            if (_port <= 0) _port = 8080;
        }

        private static int ParseInt(object? obj, int defaultVal)
        {
            if (obj is long l) return (int)l;
            if (obj is int i) return i;
            if (obj is string s && int.TryParse(s, out var v)) return v;
            return defaultVal;
        }

        private static List<Credential> ParseCredentials(object? credsObj)
        {
            var result = new List<Credential>();
            if (credsObj is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> d)
                    {
                        var user = d.TryGetValue("username", out var u) ? u?.ToString() ?? "" : "";
                        var password = d.TryGetValue("password", out var p) ? p?.ToString() ?? "" : "";
                        if (!string.IsNullOrEmpty(user))
                            result.Add(new Credential(user, password));
                    }
                }
            }
            return result;
        }

        private static List<Credential> ParseCredentialsFromJson(JsonElement el)
        {
            var result = new List<Credential>();
            if (el.ValueKind != JsonValueKind.Array) return result;
            foreach (var item in el.EnumerateArray())
            {
                var user = item.TryGetProperty("user", out var u) ? u.GetString() ?? "" : "";
                var password = item.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";
                if (!string.IsNullOrEmpty(user))
                    result.Add(new Credential(user, password));
            }
            return result;
        }

        public override Task<DeviceOperationResult> OpenAsync()
        {
            if (_port <= 0) _port = 8080;

            _cts?.Cancel();
            _cts?.Dispose();

            // OpenAsync should not start listening/serving; we start in AwaitObjAsync().
            _cts = new CancellationTokenSource();
            _listener = null;
            _serverTask = null;

            return Task.FromResult(DeviceOperationResult.Success);
        }

        public override async Task<object?> AwaitObjAsync()
        {
            // await claw1 must block until the listener is actually stopped.
            EnsureServerStarted();

            if (_serverTask != null)
            {
                Console.WriteLine($"[{DateTime.UtcNow:o}] {LogPrefix} await entered. Waiting for listener stop...");
                await _serverTask.ConfigureAwait(false);
                Console.WriteLine($"[{DateTime.UtcNow:o}] {LogPrefix} await released. Listener stopped.");
            }

            return this;
        }

        public override Task<object?> Await()
            => AwaitObjAsync();

        private void EnsureServerStarted()
        {
            // Start server only once; repeated await should wait on same task.
            if (_serverTask != null && !_serverTask.IsCompleted)
                return;

            _cts ??= new CancellationTokenSource();

            HttpListenerException? lastException = null;
            string? lastPrefix = null;

            var prefixes = new List<string>
            {
                $"http://localhost:{_port}/",
                $"http://127.0.0.1:{_port}/",
                $"http://+:{_port}/"
            };

            foreach (var prefix in prefixes)
            {
                HttpListener? listener = null;
                lastPrefix = prefix;
                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    _listener = listener;
                    _serverTask = RunServerAsync(_cts.Token);

                    Console.WriteLine(
                        $"[{DateTime.UtcNow:o}] {LogPrefix} started. Listening on '{prefix}' (port={_port}).");
                    return;
                }
                catch (HttpListenerException ex)
                {
                    lastException = ex;
                    Console.WriteLine(
                        $"[{DateTime.UtcNow:o}] {LogPrefix} failed to bind prefix '{prefix}': {ex.Message}");
                    try { listener?.Close(); } catch { }
                }
            }

            var principal = string.IsNullOrWhiteSpace(Environment.UserDomainName)
                ? Environment.UserName
                : $"{Environment.UserDomainName}\\{Environment.UserName}";
            var urlAclPrefix = lastPrefix ?? $"http://+:{_port}/";

            if (lastException != null &&
                lastException.Message != null &&
                lastException.Message.IndexOf("Access is denied", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException(
                    $"Failed to start Claw listener on port {_port}: {lastException.Message}. " +
                    $"Run (as admin) to reserve URLACL: netsh http add urlacl url=\"{urlAclPrefix}\" user=\"{principal}\"");
            }

            throw new InvalidOperationException(
                $"Failed to start Claw listener on port {_port}: {lastException?.Message ?? "unknown error"}");
        }

        public override async Task<DeviceOperationResult> CloseAsync()
        {
            _cts?.Cancel();
            _listener?.Stop();
            if (_serverTask != null)
            {
                try { await _serverTask.ConfigureAwait(false); } catch { }
            }

            Console.WriteLine($"[{DateTime.UtcNow:o}] {LogPrefix} stopped. (port={_port})");
            return DeviceOperationResult.Success;
        }

        private async Task RunServerAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener!.GetContextAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException) { break; }
                catch (HttpListenerException) { break; }
                catch { continue; }

                _ = Task.Run(() => HandleRequestAsync(ctx), ct);
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext ctx)
        {
            var sw = Stopwatch.StartNew();
            var req = ctx.Request;
            var resp = ctx.Response;

            var remote = req.RemoteEndPoint?.ToString() ?? "(unknown)";
            var absoluteUrl = req.Url?.AbsoluteUri ?? "(null-url)";
            var method = req.HttpMethod ?? "(null-method)";
            var path = req.Url?.AbsolutePath?.TrimEnd('/') ?? "";

            var statusCode = 0;

            Console.WriteLine(
                $"[{DateTime.UtcNow:o}] {LogPrefix} request. remote={remote}, method={method}, url={absoluteUrl}");

            try
            {
                resp.ContentType = "application/json";

                if (req.HttpMethod != "POST")
                {
                    statusCode = await WriteJsonResponse(resp, 405,
                            new Dictionary<string, object> { ["error"] = "Method Not Allowed" })
                        .ConfigureAwait(false);
                    return;
                }

                if (string.Equals(path, "/authenticate", StringComparison.OrdinalIgnoreCase))
                {
                    statusCode = await HandleAuthenticateAsync(req, resp).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(path, "/entrypoint", StringComparison.OrdinalIgnoreCase))
                {
                    statusCode = await HandleEntrypointAsync(req, resp).ConfigureAwait(false);
                    return;
                }

                statusCode = await WriteJsonResponse(resp, 404,
                        new Dictionary<string, object> { ["error"] = "Not Found" })
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[{DateTime.UtcNow:o}] {LogPrefix} request failed. url={absoluteUrl}, error={ex}");
                try
                {
                    statusCode = await WriteJsonResponse(ctx.Response, 500,
                            new Dictionary<string, object> { ["error"] = ex.Message })
                        .ConfigureAwait(false);
                }
                catch { }
            }
            finally
            {
                Console.WriteLine(
                    $"[{DateTime.UtcNow:o}] {LogPrefix} request done. url={absoluteUrl}, status={statusCode}, elapsed_ms={sw.ElapsedMilliseconds}");
            }
        }

        private async Task<int> HandleAuthenticateAsync(HttpListenerRequest req, HttpListenerResponse resp)
        {
            var remote = req.RemoteEndPoint?.ToString() ?? "(unknown)";
            var body = await ReadBodyAsync(req).ConfigureAwait(false);
            string? user = null;
            string? password = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("username", out var u)) user = u.GetString();
                if (root.TryGetProperty("password", out var p)) password = p.GetString();
            }
            catch
            {
                return await WriteJsonResponse(resp, 400,
                        new Dictionary<string, object> { ["error"] = "Invalid JSON" })
                    .ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(user))
            {
                return await WriteJsonResponse(resp, 400,
                        new Dictionary<string, object> { ["error"] = "user required" })
                    .ConfigureAwait(false);
            }

            // Check lockout
            if (_lockouts.TryGetValue(user, out var lockout) && lockout.IsLocked())
            {
                var remaining = lockout.RemainingSeconds();
                Console.WriteLine(
                    $"[{DateTime.UtcNow:o}] {LogPrefix} /authenticate rejected (locked). remote={remote}, user={user}, retry_after_seconds={remaining}");
                return await WriteJsonResponse(resp, 429, new Dictionary<string, object>
                {
                    ["error"] = "Too many failed attempts. Account locked.",
                    ["retry_after_seconds"] = (object)remaining
                }).ConfigureAwait(false);
            }

            // Validate credentials
            var found = _credentials.Find(c => string.Equals(c.User, user, StringComparison.OrdinalIgnoreCase) && c.Password == password);
            if (found == null)
            {
                var lo = _lockouts.GetOrAdd(user, _ => new UserLockout());
                lo.RecordFailure();
                Console.WriteLine(
                    $"[{DateTime.UtcNow:o}] {LogPrefix} /authenticate rejected (bad credentials). remote={remote}, user={user}");
                return await WriteJsonResponse(resp, 401,
                        new Dictionary<string, object> { ["error"] = "Invalid credentials" })
                    .ConfigureAwait(false);
            }

            if (_lockouts.TryGetValue(user, out var existingLockout))
                existingLockout.Reset();

            var jwt = GenerateJwtToken(user);
            // Keep mapping for backward compatibility (accept old stateful tokens if needed).
            _tokens[jwt] = user;

            Console.WriteLine(
                $"[{DateTime.UtcNow:o}] {LogPrefix} /authenticate ok. remote={remote}, user={user}");
            return await WriteJsonResponse(resp, 200,
                    // Return full header value so clients can just do:
                    // Authorization: {{loginReq.response.body.token}}
                    new Dictionary<string, object> { ["token"] = (object)$"Bearer {jwt}" })
                .ConfigureAwait(false);
        }

        private async Task<int> HandleEntrypointAsync(HttpListenerRequest req, HttpListenerResponse resp)
        {
            var authHeader = req.Headers["Authorization"] ?? "";

            var jwtToken = ExtractBearerToken(authHeader);
            if (string.IsNullOrEmpty(jwtToken))
            {
                Console.WriteLine(
                    $"[{DateTime.UtcNow:o}] {LogPrefix} /entrypoint rejected (unauthorized). remote={req.RemoteEndPoint}, url={req.Url?.AbsoluteUri}");
                return await WriteJsonResponse(resp, 401,
                        new Dictionary<string, object> { ["error"] = "Unauthorized" })
                    .ConfigureAwait(false);
            }

            var user = TryGetUserFromJwt(jwtToken, out var fromJwt) ? fromJwt : null;
            if (user == null && _tokens.TryGetValue(jwtToken, out var fromDict))
                user = fromDict;

            if (string.IsNullOrEmpty(user))
            {
                Console.WriteLine(
                    $"[{DateTime.UtcNow:o}] {LogPrefix} /entrypoint rejected (unauthorized). remote={req.RemoteEndPoint}, url={req.Url?.AbsoluteUri}");
                return await WriteJsonResponse(resp, 401,
                        new Dictionary<string, object> { ["error"] = "Unauthorized" })
                    .ConfigureAwait(false);
            }

            var body = await ReadBodyAsync(req).ConfigureAwait(false);

            string? methodName = null;
            object? data = null;

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("method", out var m)) methodName = m.GetString();
                if (root.TryGetProperty("data", out var d)) data = JsonElementToObject(d);
            }
            catch
            {
                return await WriteJsonResponse(resp, 400,
                        new Dictionary<string, object> { ["error"] = "Invalid JSON" })
                    .ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(methodName))
            {
                return await WriteJsonResponse(resp, 400,
                        new Dictionary<string, object> { ["error"] = "method required" })
                    .ConfigureAwait(false);
            }

            if (!_methodMap.TryGetValue(methodName, out var procedureName))
            {
                return await WriteJsonResponse(resp, 404,
                        new Dictionary<string, object> { ["error"] = $"Method '{methodName}' not found" })
                    .ConfigureAwait(false);
            }

            Console.WriteLine(
                $"[{DateTime.UtcNow:o}] {LogPrefix} /entrypoint. remote={req.RemoteEndPoint}, user={user}, method={methodName}, url={req.Url?.AbsoluteUri}");

            // Build socket context for this request
            var remoteIp = req.RemoteEndPoint?.Address?.ToString() ?? "";
            var remotePort = req.RemoteEndPoint?.Port ?? 0;

            var socketCtx = new ClawSocketContext(
                string.IsNullOrWhiteSpace(Name) ? "claw" : Name.Trim(),
                user,
                jwtToken,
                remoteIp,
                remotePort);

            // Build call data passed as procedure argument
            var callData = new Dictionary<string, object>
            {
                ["authentication"] = new Dictionary<string, object>
                {
                    ["isAuthenticated"] = (object)true,
                    ["user"] = (object)user,
                    // Raw JWT (without Bearer scheme).
                    ["token"] = (object)jwtToken
                },
                ["command"] = (object)(data is Dictionary<string, object> dataDict && dataDict.TryGetValue("command", out var cmd) ? cmd?.ToString() ?? "" : ""),
                ["data"] = data ?? (object)new Dictionary<string, object>(),
                // Keep socket in payload for procedures that expect data.socket.
                ["socket"] = socketCtx
            };

            object? result = null;
            try
            {
                result = await InvokeProcedureAsync(procedureName, callData, socketCtx).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return await WriteJsonResponse(resp, 500,
                        new Dictionary<string, object> { ["error"] = (object)ex.Message })
                    .ConfigureAwait(false);
            }

            var responseObj = result as Dictionary<string, object> ?? new Dictionary<string, object> { ["result"] = result ?? (object)"ok" };

            Console.WriteLine(
                $"[{DateTime.UtcNow:o}] {LogPrefix} /entrypoint result ready. user={user}, method={methodName}");
            return await WriteJsonResponse(resp, 200, responseObj).ConfigureAwait(false);
        }

        /// <summary>Invokes a registered AGI procedure in a new interpreter instance sharing the same compiled unit.</summary>
        private async Task<object?> InvokeProcedureAsync(string procedureName, object? data, ClawSocketContext socketCtx)
        {
            if (_unit == null)
                throw new InvalidOperationException("Claw device not opened or no execution unit available.");

            // Set socket context for access inside the procedure via 'socket' keyword
            try
            {
                // Create a new interpreter for each request (thread-safe, stateless per-request)
                var interpreter = new Interpreter();
                if (_kernelConfig != null)
                    interpreter.Configuration = _kernelConfig;
                interpreter.CurrentSocketContext = socketCtx;

                var callInfo = new CallInfo { FunctionName = procedureName };
                callInfo.Parameters["0"] = data;

                var result = await interpreter.InterpreteFromEntryAsync(_unit, procedureName, callInfo).ConfigureAwait(false);
                if (!result.Success)
                    throw new InvalidOperationException($"Procedure '{procedureName}' execution failed.");

                // Extract top-of-stack return value if available
                return interpreter.Stack.Count > 0 ? interpreter.Stack[interpreter.Stack.Count - 1] : null;
            }
            finally { }
        }

        private static async Task<string> ReadBodyAsync(HttpListenerRequest req)
        {
            using var ms = new System.IO.MemoryStream();
            await req.InputStream.CopyToAsync(ms).ConfigureAwait(false);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static async Task<int> WriteJsonResponse(HttpListenerResponse resp, int statusCode, object body)
        {
            resp.StatusCode = statusCode;
            var json = JsonSerializer.Serialize(body);
            var bytes = Encoding.UTF8.GetBytes(json);
            resp.ContentLength64 = bytes.Length;
            await resp.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            resp.OutputStream.Close();
            return statusCode;
        }

        private string GenerateJwtToken(string user)
        {
            var now = DateTime.UtcNow;
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            };

            var token = new JwtSecurityToken(
                claims: claims,
                notBefore: now,
                expires: now.Add(_jwtTtl),
                signingCredentials: signingCredentials);

            return _jwtTokenHandler.WriteToken(token);
        }

        private static string ExtractBearerToken(string authHeader)
        {
            // Supports these forms:
            // - "Bearer <jwt>"
            // - "Bearer Bearer <jwt>" (common if client uses response token as-is)
            // - "<jwt>" (best-effort)
            var s = (authHeader ?? "").Trim();
            if (s.Length == 0) return "";

            while (true)
            {
                if (s.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
                {
                    var after = s.Substring("Bearer".Length).TrimStart();
                    // If there is no actual token part, stop.
                    if (after.Length == 0 || after.Equals(s, StringComparison.Ordinal)) break;
                    s = after;
                    continue;
                }

                break;
            }

            return s;
        }

        private bool TryGetUserFromJwt(string jwtToken, out string user)
        {
            user = "";
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(10)
                };

                var principal = _jwtTokenHandler.ValidateToken(jwtToken, tokenValidationParameters, out _);
                var sub = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrWhiteSpace(sub)) return false;

                user = sub;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object? JsonElementToObject(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Object => JsonElementToDict(el),
                JsonValueKind.Array => JsonElementToList(el),
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : (object)el.GetDouble(),
                JsonValueKind.True => (object)true,
                JsonValueKind.False => (object)false,
                _ => (object?)null
            };
        }

        private static Dictionary<string, object> JsonElementToDict(JsonElement el)
        {
            var d = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var prop in el.EnumerateObject())
            {
                var v = JsonElementToObject(prop.Value);
                if (v != null) d[prop.Name] = v;
            }
            return d;
        }

        private static List<object> JsonElementToList(JsonElement el)
        {
            var list = new List<object>();
            foreach (var item in el.EnumerateArray())
            {
                var v = JsonElementToObject(item);
                if (v != null) list.Add(v);
            }
            return list;
        }

        // Unimplemented stream I/O — Claw is a server-mode device
        public override Task<(DeviceOperationResult Result, byte[] Bytes)> ReadAsync()
            => Task.FromResult((DeviceOperationResult.Fail(DeviceOperationState.Failed, "Not supported"), Array.Empty<byte>()));

        public override Task<DeviceOperationResult> WriteAsync(byte[] bytes)
            => Task.FromResult(DeviceOperationResult.Fail(DeviceOperationState.Failed, "Not supported"));

        public override Task<DeviceOperationResult> ControlAsync(DeviceControlBase deviceControl)
            => Task.FromResult(DeviceOperationResult.Success);

        public override Task<(DeviceOperationResult Result, IStreamChunk? Chunk)> ReadChunkAsync()
            => Task.FromResult<(DeviceOperationResult, IStreamChunk?)>((DeviceOperationResult.Fail(DeviceOperationState.Failed, "Not supported"), null));

        public override Task<DeviceOperationResult> WriteChunkAsync(IStreamChunk chunk)
            => Task.FromResult(DeviceOperationResult.Fail(DeviceOperationState.Failed, "Not supported"));

        public override Task<DeviceOperationResult> MoveAsync(StructurePosition? position)
            => Task.FromResult(DeviceOperationResult.Success);

        public override Task<(DeviceOperationResult Result, long Length)> LengthAsync()
            => Task.FromResult((DeviceOperationResult.Success, 0L));

        private sealed record Credential(string User, string Password);
    }

    /// <summary>Progressive lockout durations: 10s, 30s, 1m, 5m, 15m, 30m, 1h, 1d, permanent.</summary>
    internal sealed class UserLockout
    {
        private static readonly TimeSpan[] Durations =
        {
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1),
            TimeSpan.FromDays(1),
            TimeSpan.MaxValue  // permanent
        };

        private int _failureCount;
        private DateTime _lockedUntil = DateTime.MinValue;

        public void RecordFailure()
        {
            var idx = Math.Min(_failureCount, Durations.Length - 1);
            var duration = Durations[idx];
            _lockedUntil = duration == TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.UtcNow.Add(duration);
            _failureCount++;
        }

        public bool IsLocked() => DateTime.UtcNow < _lockedUntil;

        public long RemainingSeconds()
        {
            if (_lockedUntil == DateTime.MaxValue) return long.MaxValue;
            var remaining = _lockedUntil - DateTime.UtcNow;
            return remaining.Ticks > 0 ? (long)remaining.TotalSeconds + 1 : 0;
        }

        public void Reset()
        {
            _failureCount = 0;
            _lockedUntil = DateTime.MinValue;
        }
    }

    /// <summary>Registry for claw method bindings: external method name => AGI procedure name.</summary>
    public sealed class ClawMethodsRegistry : IDefType
    {
        private readonly ConcurrentDictionary<string, string> _map;

        public long? Index { get; set; }
        public string Name { get; set; } = "methods";
        public StructurePosition? Position { get; set; }
        public List<IDefType> Generalizations { get; set; } = new List<IDefType>();

        public ClawMethodsRegistry(ConcurrentDictionary<string, string> map)
        {
            _map = map;
        }

        public Task<object?> CallObjAsync(string methodName, object?[] args)
        {
            var name = methodName?.Trim() ?? "";

            if (string.Equals(name, "add", StringComparison.OrdinalIgnoreCase))
            {
                // args[0] = external method name (string), args[1] = AGI procedure address literal (&call)
                if (args == null || args.Length < 2)
                    throw new ArgumentException("methods.add requires (methodName, &procedureName)");

                var bindName = args[0]?.ToString() ?? "";
                string procName;
                if (args[1] is Processor.AddressLiteral addressLiteral)
                    procName = addressLiteral.Address;
                else
                {
                    // Backward compatibility with old bytecode where "&call" was passed as string.
                    var rawProcRef = args[1]?.ToString() ?? "";
                    procName = rawProcRef.StartsWith("&", StringComparison.Ordinal) ? rawProcRef.Substring(1) : rawProcRef;
                }
                if (!string.IsNullOrEmpty(bindName) && !string.IsNullOrEmpty(procName))
                    _map[bindName] = procName;

                return Task.FromResult<object?>(this);
            }

            throw new CallUnknownMethodException(name, this);
        }

        public Task<object?> AwaitObjAsync() => Task.FromResult<object?>(this);
        public Task<object?> Await() => Task.FromResult<object?>(this);
        public Task<(bool IsEnd, object? Delta, object? Aggregate)> StreamWaitAsync(string streamWaitType)
            => Task.FromResult<(bool, object?, object?)>((true, null, null));
    }

    /// <summary>Context injected as the 'socket' variable inside a claw-called procedure.</summary>
    public sealed class ClawSocketContext : IDefType
    {
        public long? Index { get; set; }
        /// <summary>Client socket identity (ip:port).</summary>
        public string Name { get; set; }
        public StructurePosition? Position { get; set; }
        public List<IDefType> Generalizations { get; set; } = new List<IDefType>();

        /// <summary>Name of the claw stream/server instance.</summary>
        public string ServerName { get; }

        /// <summary>Name of the authenticated user for this connection.</summary>
        public string User { get; }

        /// <summary>Alias to User for AGI compatibility: socket.login.</summary>
        public string Login => User;

        /// <summary>Bearer token for this session.</summary>
        public string Token { get; }

        /// <summary>Remote client IP address.</summary>
        public string Ip { get; }

        /// <summary>Remote client source port.</summary>
        public int Port { get; }

        public ClawSocketContext(string serverName, string user, string token, string ip, int port)
        {
            ServerName = string.IsNullOrWhiteSpace(serverName) ? "claw" : serverName.Trim();
            User = user;
            Token = token;
            Ip = ip ?? "";
            Port = port;
            Name = !string.IsNullOrWhiteSpace(Ip) && Port > 0
                ? $"{Ip}:{Port}"
                : !string.IsNullOrWhiteSpace(Ip)
                    ? Ip
                    : !string.IsNullOrWhiteSpace(User)
                        ? User
                        : "client";
        }

        public Task<object?> CallObjAsync(string methodName, object?[] args)
            => throw new CallUnknownMethodException(methodName, this);

        public Task<object?> AwaitObjAsync() => Task.FromResult<object?>(this);
        public Task<object?> Await() => Task.FromResult<object?>(this);
        public Task<(bool IsEnd, object? Delta, object? Aggregate)> StreamWaitAsync(string streamWaitType)
            => Task.FromResult<(bool, object?, object?)>((true, null, null));
    }

}

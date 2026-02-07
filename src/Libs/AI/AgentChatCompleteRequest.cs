using AI.Interruptions;
using AI.OpenAI;
using Newtonsoft.Json;

namespace AI
{
    public class AgentChatCompleteRequest
    {
        public bool IsStart { get; set; }
        public string ApiToken { get; set; }

        public string AgentName { get; set; }
        public string State { get; set; }
        public int Version { get; set; }
        public string? CompleteUrl { get; set; }
        public string? FunctionsUrl { get; set; }
        public bool IsCallback { get; set; }
        public decimal? Temperature { get; set; } = 0;
        public int? K { get; set; }

        public OpenAIMessage Message { get; set; }
        public List<OpenAIMessage> Messages { get; set; }
        public List<object>? Functions { get; set; }

        public UserInfo? User { get; set; }
        public ContactInfo? Contact { get; set; }
        public FileInfo? File { get; set; }
        public AgentInfo? Agent { get; set; }
        public AgentRunInfo? AgentRun { get; set; }

        public async Task<AgentChatCompleteResponse> Respond(string text)
        {
            var affectedMessages = new List<OpenAIMessage>();
            var messages = Messages;
            messages.Add(Message);
            affectedMessages.Add(Message);

            var resultMessage = new OpenAIMessage
            {
                Role = "assistant",
                Content = text
            };
            messages.Add(resultMessage);
            affectedMessages.Add(resultMessage);

            return new AgentChatCompleteResponse
            {
                AgentName = AgentName,
                Message = resultMessage,
                Messages = messages,
                AffectedMessages = affectedMessages,
                State = State
            };
        }

        public async Task<AgentChatCompleteResponse> CallTransferAgent(string agentName, AgentArgs? args = null, string text = null)
        {
            return await Call("__transferAgent", JsonConvert.SerializeObject(new TransferAgentData
            {
                AgentName = agentName,
                Args = args
            }), text);
        }

        public async Task<AgentChatCompleteResponse> CallFinishAgent(string text = null)
        {
            return await Call("__finishAgent", JsonConvert.SerializeObject(new FinishAgentData
            {                
            }), text);
        }

        public async Task<AgentChatCompleteResponse> Call(string functionName, string args, string text = null)
        {
            var affectedMessages = new List<OpenAIMessage>();
            var messages = Messages;
            messages.Add(Message);
            affectedMessages.Add(Message);

            var resultMessage = new OpenAIMessage
            {
                Role = "assistant",
                Content = text,
                FunctionCall = new OpenAIFunctionCall
                {
                    Name = functionName,
                    Arguments = args
                }
            };
            messages.Add(resultMessage);
            affectedMessages.Add(resultMessage);

            return new AgentChatCompleteResponse
            {
                AgentName = AgentName,
                Message = resultMessage,
                Messages = messages,
                AffectedMessages = affectedMessages,
                State = State
            };
        }


        public async Task<AgentChatCompleteResponse> Respond(Func<AgentChatCompleteRequest, Task<AgentChatCompleteResponse>> callServer,
            Func<AgentMethodCallRequest, Task<AgentMethodCallResponse>> callFunction)
        {
            var affectedMessages = new List<OpenAIMessage>();
            var messages = Messages;
            messages.Add(Message);
            affectedMessages.Add(Message);

            var response = await callServer(this);            

            if (response?.Data != null && AgentRun != null)
            {
                AgentRun.Data = response.Data;
            }

            var result = response.Message;
            OpenAIMessage? functionCallMessage = null;
            AgentChatInterrupt? interrupt = null;

            var state = State;

            if (result?.FunctionCall != null)
            {
                functionCallMessage = result;

                var functionName = result.FunctionCall.Name;
                var arguments = result.FunctionCall.Arguments;

                AgentMethodCallResponse functionResult = null;

                var callRequest = new AgentMethodCallRequest
                {
                    Request = this,
                    AgentName = AgentName,
                    FunctionName = functionName,
                    Args = arguments,
                    FunctionsUrl = FunctionsUrl,
                    User = User
                };

                functionResult = await callFunction(callRequest);
                state = functionResult.State;

                if (functionResult.Interrupt)
                {
                    interrupt = new AgentChatInterrupt
                    {
                        AgentName = AgentName,
                        FunctionName = functionName,
                        Args = arguments,
                    };
                }

                if (state == "finish")
                {
                    interrupt = new AgentChatInterrupt
                    {
                        AgentName = AgentName,
                        FunctionName = InterruptionSystem.FinishAgent,
                        Args = "{ \"success\": true }",
                    };
                }

                if (!string.IsNullOrEmpty(functionResult.Content))
                {
                    result.Content = $"{functionResult.Content}{Environment.NewLine}{result.Content}";
                }

                var functionMessage = new OpenAIMessage
                {
                    Role = "function",
                    Name = functionName,
                    Content = functionResult.Result
                };
                messages.Add(functionMessage);
                affectedMessages.Add(functionMessage);

                functionMessage.FunctionCall = new OpenAIFunctionCall
                {
                    Name = functionName,
                    Arguments = arguments
                };

                //if (functionName != InterruptionSystem.TransferAgent)
                //{
                //    response = await callServer(this);

                //    functionMessage.FunctionCall = new OpenAIFunctionCall
                //    {
                //        Name = functionName,
                //        Arguments = arguments
                //    };

                //    result = response.Message;
                //}
            }

            if (result != null)
            {
                var resultMessage = new OpenAIMessage
                {
                    Role = "assistant",
                    Content = result.Content
                };
                messages.Add(resultMessage);
                affectedMessages.Add(resultMessage);
            }

            return new AgentChatCompleteResponse
            {
                AgentName = AgentName,
                Message = result,
                Markup = response.Markup,
                FunctionCallMessage = functionCallMessage,
                Messages = messages,
                AffectedMessages = affectedMessages,
                State = state,
                Data = response.Data,
                Interrupt = interrupt,
                EscapeSpecialCharacters = response.EscapeSpecialCharacters,
            };
        }

        public async Task<AgentChatCompleteResponse> Respond(Func<OpenAIChatCompleteRequest, Task<OpenAIChatCompleteResponse>> callOpenAI, 
            Func<AgentMethodCallRequest, Task<AgentMethodCallResponse>> callFunction)
        {
            var affectedMessages = new List<OpenAIMessage>();
            var messages = Messages;
            messages.Add(Message);
            affectedMessages.Add(Message);

            var response = await callOpenAI(new OpenAIChatCompleteRequest
            {
                Temperature = Temperature ?? 0,
                Messages = messages,
                Functions = Functions
            });

            var result = response.Result;
            OpenAIMessage? functionCallMessage = null;
            AgentChatInterrupt? interrupt = null;

            var state = State;

            if (result?.FunctionCall != null)
            {
                functionCallMessage = result;

                var functionName = result.FunctionCall.Name;
                var arguments = result.FunctionCall.Arguments;

                AgentMethodCallResponse functionResult = null;

                var callRequest = new AgentMethodCallRequest
                {
                    Request = this,
                    AgentName = AgentName,
                    FunctionName = functionName,
                    Args = arguments,
                    FunctionsUrl = FunctionsUrl,
                    User = User
                };

                functionResult = await callFunction(callRequest);
                state = functionResult.State;

                if (functionResult.Interrupt)
                {
                    interrupt = new AgentChatInterrupt
                    {
                        AgentName = AgentName,
                        FunctionName = functionName,
                        Args = arguments,
                    };
                }

                if (state == "finish")
                {
                    interrupt = new AgentChatInterrupt
                    {
                        AgentName = AgentName,
                        FunctionName = InterruptionSystem.FinishAgent,
                        Args = "{ \"success\": true }",
                    };
                }

                var functionMessage = new OpenAIMessage
                {
                    Role = "function",
                    Name = functionName,
                    Content = functionResult.Result                    
                };
                messages.Add(functionMessage);
                affectedMessages.Add(functionMessage);

                if (functionName != InterruptionSystem.TransferAgent)
                {
                    response = await callOpenAI(new OpenAIChatCompleteRequest
                    {
                        Temperature = 0,
                        Messages = messages
                    });

                    functionMessage.FunctionCall = new OpenAIFunctionCall
                    {
                        Name = functionName,
                        Arguments = arguments
                    };

                    result = response.Result;
                }
            }

            if (result != null)
            {
                var resultMessage = new OpenAIMessage
                {
                    Role = "assistant",
                    Content = result.Content
                };
                messages.Add(resultMessage);
                affectedMessages.Add(resultMessage);
            }

            return new AgentChatCompleteResponse
            {
                AgentName = AgentName,
                Message = result,
                FunctionCallMessage = functionCallMessage,
                Messages = messages,
                AffectedMessages = affectedMessages,
                State = state,
                Interrupt = interrupt
            };
        }
    }
}

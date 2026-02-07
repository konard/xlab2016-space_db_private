using SpaceDb.Models;
using System.Xml;
using System.Xml.Linq;

namespace SpaceDb.Services.Parsers
{
    /// <summary>
    /// Parser for OWL/RDF content - converts ontology to graph structure
    /// Handles OpenCyc ontology format with classes, properties, and individuals
    /// </summary>
    public class OwlPayloadParser : PayloadParserBase
    {
        private readonly int _maxDepth;
        private readonly bool _includeAnnotations;

        // Common OWL/RDF namespaces
        private static readonly XNamespace RdfNs = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        private static readonly XNamespace RdfsNs = "http://www.w3.org/2000/01/rdf-schema#";
        private static readonly XNamespace OwlNs = "http://www.w3.org/2002/07/owl#";
        private static readonly XNamespace SkosNs = "http://www.w3.org/2004/02/skos/core#";
        private static readonly XNamespace XsdNs = "http://www.w3.org/2001/XMLSchema#";

        public override string ContentType => "owl";

        public OwlPayloadParser(
            ILogger<OwlPayloadParser> logger,
            int maxDepth = 10,
            bool includeAnnotations = true,
            int maxBlockSize = 8000) : base(logger, maxBlockSize)
        {
            _maxDepth = maxDepth;
            _includeAnnotations = includeAnnotations;
        }

        public override async Task<ParsedResource> ParseAsync(
            string payload,
            string resourceId,
            Dictionary<string, object>? metadata = null)
        {
            _logger.LogInformation("Parsing OWL/RDF payload for resource {ResourceId}", resourceId);

            var result = new ParsedResource
            {
                ResourceId = resourceId,
                ResourceType = ContentType,
                Metadata = CreateMetadata(metadata)
            };

            try
            {
                var document = XDocument.Parse(payload);
                var fragments = new List<ContentFragment>();
                int order = 0;

                // Parse ontology header
                ParseOntologyHeader(document, fragments, ref order);

                // Parse OWL Classes
                ParseOwlClasses(document, fragments, ref order);

                // Parse OWL Properties (ObjectProperty, DatatypeProperty, etc.)
                ParseOwlProperties(document, fragments, ref order);

                // Parse OWL Individuals
                ParseOwlIndividuals(document, fragments, ref order);

                // Group fragments into blocks
                result.Blocks = CreateBlocksFromFragments(fragments);

                result.Metadata["total_fragments"] = fragments.Count;
                result.Metadata["total_blocks"] = result.Blocks.Count;
                result.Metadata["owl_size"] = payload.Length;

                _logger.LogInformation("Parsed {FragmentCount} OWL/RDF nodes into {BlockCount} blocks from resource {ResourceId}",
                    fragments.Count, result.Blocks.Count, resourceId);
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "Failed to parse OWL/RDF payload for resource {ResourceId}", resourceId);
                throw new InvalidOperationException($"Invalid OWL/RDF payload: {ex.Message}", ex);
            }

            return await Task.FromResult(result);
        }

        private void ParseOntologyHeader(XDocument document, List<ContentFragment> fragments, ref int order)
        {
            var ontologies = document.Descendants(OwlNs + "Ontology");

            foreach (var ontology in ontologies)
            {
                var about = ontology.Attribute(RdfNs + "about")?.Value ?? "unknown";
                var label = ontology.Element(RdfsNs + "label")?.Value ?? "Unnamed Ontology";
                var comment = ontology.Element(RdfsNs + "comment")?.Value ?? "";
                var versionInfo = ontology.Element(OwlNs + "versionInfo")?.Value ?? "";

                var content = $"Ontology: {label}";
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    content += $"\n{comment}";
                }
                if (!string.IsNullOrWhiteSpace(versionInfo))
                {
                    content += $"\nVersion: {versionInfo}";
                }

                fragments.Add(new ContentFragment
                {
                    Content = content,
                    Type = "owl_ontology",
                    Order = order++,
                    ParentKey = null,
                    Metadata = new Dictionary<string, object>
                    {
                        ["uri"] = about,
                        ["label"] = label,
                        ["version"] = versionInfo
                    }
                });

                _logger.LogDebug("Parsed ontology: {Label}", label);
            }
        }

        private void ParseOwlClasses(XDocument document, List<ContentFragment> fragments, ref int order)
        {
            var classes = document.Descendants(OwlNs + "Class");

            foreach (var classElement in classes)
            {
                var about = classElement.Attribute(RdfNs + "about")?.Value ?? "unknown";
                var label = GetLabelValue(classElement);
                var comment = classElement.Element(RdfsNs + "comment")?.Value ?? "";
                var definition = classElement.Element(SkosNs + "definition")?.Value ?? "";

                // Get parent classes (subClassOf)
                var parentClasses = classElement.Elements(RdfsNs + "subClassOf")
                    .Select(e => e.Attribute(RdfNs + "resource")?.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(ExtractLocalName)
                    .ToList();

                // Get sameAs links
                var sameAsLinks = classElement.Elements(OwlNs + "sameAs")
                    .Select(e => e.Attribute(RdfNs + "resource")?.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();

                // Build content
                var content = $"Class: {label}";
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    content += $"\nDefinition: {definition}";
                }
                else if (!string.IsNullOrWhiteSpace(comment))
                {
                    content += $"\nComment: {comment}";
                }

                if (parentClasses.Any())
                {
                    content += $"\nSubclass of: {string.Join(", ", parentClasses)}";
                }

                if (sameAsLinks.Any())
                {
                    content += $"\nSame as: {string.Join(", ", sameAsLinks.Select(ExtractLocalName))}";
                }

                var fragmentMetadata = new Dictionary<string, object>
                {
                    ["uri"] = about,
                    ["label"] = label,
                    ["owl_type"] = "class"
                };

                if (parentClasses.Any())
                {
                    fragmentMetadata["parent_classes"] = parentClasses;
                }

                if (sameAsLinks.Any())
                {
                    fragmentMetadata["same_as_links"] = sameAsLinks;
                }

                // Add GUID if present (common in OpenCyc)
                var guid = GetAnnotationValue(classElement, "guid");
                if (!string.IsNullOrWhiteSpace(guid))
                {
                    fragmentMetadata["cyc_guid"] = guid;
                }

                fragments.Add(new ContentFragment
                {
                    Content = content,
                    Type = "owl_class",
                    Order = order++,
                    ParentKey = null,
                    Metadata = fragmentMetadata
                });

                _logger.LogDebug("Parsed class: {Label}", label);
            }
        }

        private void ParseOwlProperties(XDocument document, List<ContentFragment> fragments, ref int order)
        {
            // Parse ObjectProperty, DatatypeProperty, AnnotationProperty
            var propertyTypes = new[]
            {
                "ObjectProperty",
                "DatatypeProperty",
                "AnnotationProperty",
                "FunctionalProperty",
                "InverseFunctionalProperty",
                "TransitiveProperty",
                "SymmetricProperty"
            };

            foreach (var propertyType in propertyTypes)
            {
                var properties = document.Descendants(OwlNs + propertyType);

                foreach (var property in properties)
                {
                    var about = property.Attribute(RdfNs + "about")?.Value ?? "unknown";
                    var label = GetLabelValue(property);
                    var comment = property.Element(RdfsNs + "comment")?.Value ?? "";

                    // Get domain and range
                    var domain = property.Element(RdfsNs + "domain")?.Attribute(RdfNs + "resource")?.Value;
                    var range = property.Element(RdfsNs + "range")?.Attribute(RdfNs + "resource")?.Value;

                    // Build content
                    var content = $"{propertyType}: {label}";
                    if (!string.IsNullOrWhiteSpace(comment))
                    {
                        content += $"\n{comment}";
                    }

                    if (!string.IsNullOrWhiteSpace(domain))
                    {
                        content += $"\nDomain: {ExtractLocalName(domain)}";
                    }

                    if (!string.IsNullOrWhiteSpace(range))
                    {
                        content += $"\nRange: {ExtractLocalName(range)}";
                    }

                    var fragmentMetadata = new Dictionary<string, object>
                    {
                        ["uri"] = about,
                        ["label"] = label,
                        ["owl_type"] = propertyType.ToLower()
                    };

                    if (!string.IsNullOrWhiteSpace(domain))
                    {
                        fragmentMetadata["domain"] = domain;
                    }

                    if (!string.IsNullOrWhiteSpace(range))
                    {
                        fragmentMetadata["range"] = range;
                    }

                    // Add GUID if present
                    var guid = GetAnnotationValue(property, "guid");
                    if (!string.IsNullOrWhiteSpace(guid))
                    {
                        fragmentMetadata["cyc_guid"] = guid;
                    }

                    fragments.Add(new ContentFragment
                    {
                        Content = content,
                        Type = "owl_property",
                        Order = order++,
                        ParentKey = null,
                        Metadata = fragmentMetadata
                    });

                    _logger.LogDebug("Parsed property: {Label} ({Type})", label, propertyType);
                }
            }
        }

        private void ParseOwlIndividuals(XDocument document, List<ContentFragment> fragments, ref int order)
        {
            var individuals = document.Descendants(OwlNs + "NamedIndividual");

            foreach (var individual in individuals)
            {
                var about = individual.Attribute(RdfNs + "about")?.Value ?? "unknown";
                var label = GetLabelValue(individual);
                var comment = individual.Element(RdfsNs + "comment")?.Value ?? "";

                // Get rdf:type (the class this individual belongs to)
                var types = individual.Elements(RdfNs + "type")
                    .Select(e => e.Attribute(RdfNs + "resource")?.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(ExtractLocalName)
                    .ToList();

                // Build content
                var content = $"Individual: {label}";
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    content += $"\n{comment}";
                }

                if (types.Any())
                {
                    content += $"\nType: {string.Join(", ", types)}";
                }

                var fragmentMetadata = new Dictionary<string, object>
                {
                    ["uri"] = about,
                    ["label"] = label,
                    ["owl_type"] = "individual"
                };

                if (types.Any())
                {
                    fragmentMetadata["types"] = types;
                }

                fragments.Add(new ContentFragment
                {
                    Content = content,
                    Type = "owl_individual",
                    Order = order++,
                    ParentKey = null,
                    Metadata = fragmentMetadata
                });

                _logger.LogDebug("Parsed individual: {Label}", label);
            }
        }

        /// <summary>
        /// Get label value, handling xml:lang attribute
        /// </summary>
        private string GetLabelValue(XElement element)
        {
            var labelElement = element.Element(RdfsNs + "label");
            if (labelElement != null)
            {
                return labelElement.Value;
            }

            // Fallback to extracting from rdf:about
            var about = element.Attribute(RdfNs + "about")?.Value;
            return about != null ? ExtractLocalName(about) : "Unnamed";
        }

        /// <summary>
        /// Extract local name from URI (e.g., "Cx-Dog" from "http://sw.opencyc.org/concept/Cx-Dog")
        /// </summary>
        private string ExtractLocalName(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return "unknown";

            var lastSlash = uri.LastIndexOf('/');
            var lastHash = uri.LastIndexOf('#');
            var separatorIndex = Math.Max(lastSlash, lastHash);

            if (separatorIndex >= 0 && separatorIndex < uri.Length - 1)
            {
                return uri.Substring(separatorIndex + 1);
            }

            return uri;
        }

        /// <summary>
        /// Get annotation value from OpenCyc-specific annotations (cycAnnot namespace)
        /// </summary>
        private string? GetAnnotationValue(XElement element, string annotationName)
        {
            if (!_includeAnnotations)
                return null;

            // Try to find annotation in any namespace ending with "Annotations"
            var annotation = element.Elements()
                .FirstOrDefault(e => e.Name.LocalName == annotationName &&
                                     (e.Name.Namespace.NamespaceName.Contains("Annotation") ||
                                      e.Name.Namespace.NamespaceName.Contains("cyc")));

            return annotation?.Value;
        }

        public override bool CanParse(string payload)
        {
            if (!base.CanParse(payload))
                return false;

            try
            {
                var document = XDocument.Parse(payload);

                // Check if root element is rdf:RDF
                if (document.Root?.Name.LocalName == "RDF" &&
                    document.Root.Name.Namespace == RdfNs)
                {
                    // Check if it contains OWL elements
                    var hasOwlElements = document.Descendants()
                        .Any(e => e.Name.Namespace == OwlNs);

                    return hasOwlElements;
                }

                return false;
            }
            catch (XmlException)
            {
                return false;
            }
        }
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceDb.Services.Parsers;
using Xunit;

namespace SpaceDb.Tests.Services.Parsers
{
    public class OwlPayloadParserTests
    {
        private readonly Mock<ILogger<OwlPayloadParser>> _loggerMock;
        private readonly OwlPayloadParser _parser;

        public OwlPayloadParserTests()
        {
            _loggerMock = new Mock<ILogger<OwlPayloadParser>>();
            _parser = new OwlPayloadParser(_loggerMock.Object);
        }

        #region Basic Functionality Tests

        [Fact]
        public void ContentType_ShouldReturnOwl()
        {
            // Act
            var contentType = _parser.ContentType;

            // Assert
            contentType.Should().Be("owl");
        }

        [Fact]
        public void CanParse_WithValidOwl_ShouldReturnTrue()
        {
            // Arrange
            var validOwl = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:Class rdf:about=""http://example.org/Thing""/>
</rdf:RDF>";

            // Act
            var result = _parser.CanParse(validOwl);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void CanParse_WithInvalidXml_ShouldReturnFalse()
        {
            // Arrange
            var invalidXml = "This is not XML";

            // Act
            var result = _parser.CanParse(invalidXml);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void CanParse_WithXmlButNotOwl_ShouldReturnFalse()
        {
            // Arrange
            var xmlNotOwl = @"<?xml version=""1.0""?>
<root>
    <element>Some content</element>
</root>";

            // Act
            var result = _parser.CanParse(xmlNotOwl);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void CanParse_WithEmptyString_ShouldReturnFalse()
        {
            // Act
            var result = _parser.CanParse("");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Ontology Header Parsing Tests

        [Fact]
        public async Task ParseAsync_WithOntologyHeader_ShouldCreateOntologyFragment()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:Ontology rdf:about=""http://example.org/ontology"">
        <rdfs:label>Test Ontology</rdfs:label>
        <rdfs:comment>A test ontology for unit testing</rdfs:comment>
        <owl:versionInfo>1.0</owl:versionInfo>
    </owl:Ontology>
</rdf:RDF>";
            var resourceId = "test_ontology";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Should().NotBeNull();
            result.ResourceId.Should().Be(resourceId);
            result.ResourceType.Should().Be("owl");

            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().ContainSingle(f => f.Type == "owl_ontology");

            var ontologyFragment = allFragments.First(f => f.Type == "owl_ontology");
            ontologyFragment.Content.Should().Contain("Test Ontology");
            ontologyFragment.Content.Should().Contain("A test ontology for unit testing");
            ontologyFragment.Content.Should().Contain("Version: 1.0");
            ontologyFragment.Metadata.Should().ContainKey("uri");
            ontologyFragment.Metadata.Should().ContainKey("label");
            ontologyFragment.Metadata.Should().ContainKey("version");
        }

        #endregion

        #region Class Parsing Tests

        [Fact]
        public async Task ParseAsync_WithOwlClass_ShouldCreateClassFragment()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:Class rdf:about=""http://example.org/Dog"">
        <rdfs:label xml:lang=""en"">Dog</rdfs:label>
        <rdfs:comment>A domesticated carnivorous mammal</rdfs:comment>
    </owl:Class>
</rdf:RDF>";
            var resourceId = "test_class";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().ContainSingle(f => f.Type == "owl_class");

            var classFragment = allFragments.First(f => f.Type == "owl_class");
            classFragment.Content.Should().Contain("Class: Dog");
            classFragment.Content.Should().Contain("A domesticated carnivorous mammal");
            classFragment.Metadata.Should().ContainKey("uri");
            classFragment.Metadata.Should().ContainKey("label");
            classFragment.Metadata["owl_type"].Should().Be("class");
        }

        [Fact]
        public async Task ParseAsync_WithClassHierarchy_ShouldIncludeSubClassOf()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:Class rdf:about=""http://example.org/Dog"">
        <rdfs:label>Dog</rdfs:label>
        <rdfs:subClassOf rdf:resource=""http://example.org/Animal""/>
        <rdfs:subClassOf rdf:resource=""http://example.org/Pet""/>
    </owl:Class>
</rdf:RDF>";
            var resourceId = "test_class_hierarchy";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            var classFragment = allFragments.First(f => f.Type == "owl_class");

            classFragment.Content.Should().Contain("Subclass of:");
            classFragment.Content.Should().Contain("Animal");
            classFragment.Content.Should().Contain("Pet");
            classFragment.Metadata.Should().ContainKey("parent_classes");
        }

        [Fact]
        public async Task ParseAsync_WithSameAsLinks_ShouldIncludeThem()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:Class rdf:about=""http://sw.opencyc.org/concept/Cx-Dog"">
        <rdfs:label>Dog</rdfs:label>
        <owl:sameAs rdf:resource=""http://dbpedia.org/resource/Dog""/>
        <owl:sameAs rdf:resource=""http://umbel.org/umbel/rc/Dog""/>
    </owl:Class>
</rdf:RDF>";
            var resourceId = "test_same_as";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            var classFragment = allFragments.First(f => f.Type == "owl_class");

            classFragment.Content.Should().Contain("Same as:");
            classFragment.Content.Should().Contain("Dog");
            classFragment.Metadata.Should().ContainKey("same_as_links");
        }

        [Fact]
        public async Task ParseAsync_WithSkosDefinition_ShouldPreferDefinitionOverComment()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#""
         xmlns:skos=""http://www.w3.org/2004/02/skos/core#"">
    <owl:Class rdf:about=""http://example.org/Dog"">
        <rdfs:label>Dog</rdfs:label>
        <rdfs:comment>This is a comment</rdfs:comment>
        <skos:definition>A domesticated descendant of the wolf</skos:definition>
    </owl:Class>
</rdf:RDF>";
            var resourceId = "test_skos_definition";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            var classFragment = allFragments.First(f => f.Type == "owl_class");

            classFragment.Content.Should().Contain("Definition: A domesticated descendant of the wolf");
            classFragment.Content.Should().NotContain("Comment:");
        }

        #endregion

        #region Property Parsing Tests

        [Fact]
        public async Task ParseAsync_WithObjectProperty_ShouldCreatePropertyFragment()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:ObjectProperty rdf:about=""http://example.org/hasOwner"">
        <rdfs:label>has owner</rdfs:label>
        <rdfs:comment>Relates a pet to its owner</rdfs:comment>
        <rdfs:domain rdf:resource=""http://example.org/Pet""/>
        <rdfs:range rdf:resource=""http://example.org/Person""/>
    </owl:ObjectProperty>
</rdf:RDF>";
            var resourceId = "test_property";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().ContainSingle(f => f.Type == "owl_property");

            var propertyFragment = allFragments.First(f => f.Type == "owl_property");
            propertyFragment.Content.Should().Contain("ObjectProperty: has owner");
            propertyFragment.Content.Should().Contain("Domain: Pet");
            propertyFragment.Content.Should().Contain("Range: Person");
            propertyFragment.Metadata["owl_type"].Should().Be("objectproperty");
        }

        [Fact]
        public async Task ParseAsync_WithDatatypeProperty_ShouldCreatePropertyFragment()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:DatatypeProperty rdf:about=""http://example.org/age"">
        <rdfs:label>age</rdfs:label>
        <rdfs:comment>The age in years</rdfs:comment>
    </owl:DatatypeProperty>
</rdf:RDF>";
            var resourceId = "test_datatype_property";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            var propertyFragment = allFragments.First(f => f.Type == "owl_property");

            propertyFragment.Content.Should().Contain("DatatypeProperty: age");
            propertyFragment.Metadata["owl_type"].Should().Be("datatypeproperty");
        }

        #endregion

        #region Individual Parsing Tests

        [Fact]
        public async Task ParseAsync_WithNamedIndividual_ShouldCreateIndividualFragment()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:NamedIndividual rdf:about=""http://example.org/Fido"">
        <rdf:type rdf:resource=""http://example.org/Dog""/>
        <rdfs:label>Fido</rdfs:label>
        <rdfs:comment>A specific dog instance</rdfs:comment>
    </owl:NamedIndividual>
</rdf:RDF>";
            var resourceId = "test_individual";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().ContainSingle(f => f.Type == "owl_individual");

            var individualFragment = allFragments.First(f => f.Type == "owl_individual");
            individualFragment.Content.Should().Contain("Individual: Fido");
            individualFragment.Content.Should().Contain("Type: Dog");
            individualFragment.Metadata["owl_type"].Should().Be("individual");
            individualFragment.Metadata.Should().ContainKey("types");
        }

        #endregion

        #region Complex OpenCyc Format Tests

        [Fact]
        public async Task ParseAsync_WithCompleteOpenCycStructure_ShouldParseAllElements()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xml:base=""http://sw.opencyc.org/concept/""
    xmlns=""http://sw.opencyc.org/concept/""
    xmlns:owl=""http://www.w3.org/2002/07/owl#""
    xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
    xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
    xmlns:skos=""http://www.w3.org/2004/02/skos/core#""
    xmlns:cycAnnot=""http://sw.cyc.com/CycAnnotations_v1#"">

    <owl:Ontology rdf:about=""http://sw.opencyc.org/2012/05/10/concept"">
        <rdfs:label>OpenCyc Ontology</rdfs:label>
        <owl:versionInfo>Version 4.0</owl:versionInfo>
    </owl:Ontology>

    <owl:Class rdf:about=""http://sw.opencyc.org/concept/Cx-Animal"">
        <rdfs:label xml:lang=""en"">Animal</rdfs:label>
        <skos:definition>A multicellular organism</skos:definition>
        <rdfs:subClassOf rdf:resource=""http://sw.opencyc.org/concept/Cx-LivingThing""/>
        <cycAnnot:guid>bd58daa0-9c29-11b1-9dad-c379636f7270</cycAnnot:guid>
    </owl:Class>

    <owl:Class rdf:about=""http://sw.opencyc.org/concept/Cx-Dog"">
        <rdfs:label xml:lang=""en"">Dog</rdfs:label>
        <skos:definition>A domesticated descendant of the wolf</skos:definition>
        <rdfs:subClassOf rdf:resource=""http://sw.opencyc.org/concept/Cx-Animal""/>
        <owl:sameAs rdf:resource=""http://dbpedia.org/resource/Dog""/>
        <cycAnnot:guid>bd58c3a7-9c29-11b1-9dad-c379636f7270</cycAnnot:guid>
    </owl:Class>

    <owl:ObjectProperty rdf:about=""http://sw.opencyc.org/concept/Cx-hasColor"">
        <rdfs:label xml:lang=""en"">has color</rdfs:label>
        <rdfs:domain rdf:resource=""http://sw.opencyc.org/concept/Cx-PhysicalObject""/>
        <rdfs:range rdf:resource=""http://sw.opencyc.org/concept/Cx-Color""/>
        <cycAnnot:guid>c0fd4798-9c29-11b1-9dad-c379636f7270</cycAnnot:guid>
    </owl:ObjectProperty>

    <owl:NamedIndividual rdf:about=""http://sw.opencyc.org/concept/Cx-Fido"">
        <rdf:type rdf:resource=""http://sw.opencyc.org/concept/Cx-Dog""/>
        <rdfs:label xml:lang=""en"">Fido</rdfs:label>
    </owl:NamedIndividual>

</rdf:RDF>";
            var resourceId = "complete_opencyc_test";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Should().NotBeNull();
            result.ResourceId.Should().Be(resourceId);

            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();

            // Should have ontology header
            allFragments.Should().Contain(f => f.Type == "owl_ontology");

            // Should have 2 classes
            allFragments.Where(f => f.Type == "owl_class").Should().HaveCount(2);

            // Should have 1 property
            allFragments.Should().ContainSingle(f => f.Type == "owl_property");

            // Should have 1 individual
            allFragments.Should().ContainSingle(f => f.Type == "owl_individual");

            // Total should be 5 fragments
            allFragments.Should().HaveCount(5);

            // Check that Dog class has all expected information
            var dogFragment = allFragments.First(f => f.Type == "owl_class" && f.Content.Contains("Class: Dog"));
            dogFragment.Content.Should().Contain("domesticated descendant");
            dogFragment.Content.Should().Contain("Subclass of: Cx-Animal");
            dogFragment.Content.Should().Contain("Same as:");
            dogFragment.Metadata.Should().ContainKey("cyc_guid");
            dogFragment.Metadata["cyc_guid"].ToString().Should().Be("bd58c3a7-9c29-11b1-9dad-c379636f7270");
        }

        #endregion

        #region Block Creation Tests

        [Fact]
        public async Task ParseAsync_ShouldGroupFragmentsIntoBlocks()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:Class rdf:about=""http://example.org/Class1"">
        <rdfs:label>Class 1</rdfs:label>
    </owl:Class>
    <owl:Class rdf:about=""http://example.org/Class2"">
        <rdfs:label>Class 2</rdfs:label>
    </owl:Class>
</rdf:RDF>";
            var resourceId = "test_blocks";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Blocks.Should().NotBeEmpty();
            result.Blocks.Should().AllSatisfy(block =>
            {
                block.Fragments.Should().NotBeEmpty();
                block.Content.Should().NotBeNullOrWhiteSpace();
                block.Type.Should().Be("block");
            });
        }

        #endregion

        #region Metadata Tests

        [Fact]
        public async Task ParseAsync_ShouldIncludeMetadata()
        {
            // Arrange
            var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:Class rdf:about=""http://example.org/Thing""/>
</rdf:RDF>";
            var resourceId = "test_metadata";
            var customMetadata = new Dictionary<string, object>
            {
                ["source"] = "test",
                ["version"] = 1
            };

            // Act
            var result = await _parser.ParseAsync(payload, resourceId, customMetadata);

            // Assert
            result.Metadata.Should().ContainKey("parsed_at");
            result.Metadata.Should().ContainKey("parser_type");
            result.Metadata["parser_type"].Should().Be("owl");
            result.Metadata.Should().ContainKey("source");
            result.Metadata["source"].Should().Be("test");
        }

        #endregion
    }
}

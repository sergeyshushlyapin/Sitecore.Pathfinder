﻿namespace Sitecore.Pathfinder.Documents.Xml
{
  using NUnit.Framework;
  using Sitecore.Pathfinder.Documents;

  [TestFixture]
  public class XmlDocumentTests : Tests
  {
    [TestFixtureSetUp]
    public void Setup()
    {
      this.Start();
    }

    [Test]
    public void InvalidXmlTests()
    {
      var sourceFile = new SourceFile(this.Services.FileSystem, "test.txt");

      var doc = new XmlTextSnapshot(sourceFile, "<Item>", string.Empty, string.Empty);
      Assert.AreEqual(TextNode.Empty, doc.Root);

      doc = new XmlTextSnapshot(sourceFile, string.Empty, string.Empty, string.Empty);
      Assert.AreEqual(TextNode.Empty, doc.Root);
    }

    [Test]
    public void ItemTests()
    {
      var sourceFile = new SourceFile(this.Services.FileSystem, "test.txt");

      var doc = new XmlTextSnapshot(sourceFile, "<Item><Field Name=\"Text\" Value=\"123\" /></Item>", string.Empty, string.Empty);
      var root = doc.Root;
      Assert.IsNotNull(root);
      Assert.AreEqual("Item", root.Name);
      Assert.AreEqual(1, root.ChildNodes.Count);

      var field = root.ChildNodes[0];
      Assert.AreEqual("Field", field.Name);
      Assert.AreEqual("Text", field.GetAttributeValue("Name"));
      Assert.AreEqual("123", field.GetAttributeValue("Value"));
      Assert.AreEqual(0, field.ChildNodes.Count);

      var attribute = field.GetAttribute("Name");
      Assert.IsNotNull(attribute);
      Assert.AreEqual("Text", attribute.Value);
      Assert.AreEqual(0, attribute.Attributes.Count);
      Assert.AreEqual(0, attribute.ChildNodes.Count);
      Assert.AreEqual(field, attribute.Parent);
      Assert.AreEqual(field.Snapshot, attribute.Snapshot);
      Assert.AreEqual(doc, attribute.Snapshot);
    }

    [Test]
    public void ValueTests()
    {
      var sourceFile = new SourceFile(this.Services.FileSystem, "test.txt");

      var doc = new XmlTextSnapshot(sourceFile, "<Item><Field Name=\"Text\">123</Field></Item>", string.Empty, string.Empty);
      var root = doc.Root;
      var field = root.ChildNodes[0];
      Assert.AreEqual("Field", field.Name);
      Assert.AreEqual("Text", field.GetAttributeValue("Name"));
      Assert.AreEqual("123", field.GetAttributeValue("[Value]"));
      Assert.AreEqual(string.Empty, field.Value);
      Assert.AreEqual(0, field.ChildNodes.Count);
    }

    [Test]
    public void GetNestedTextNodeTests()
    {
      var sourceFile = new SourceFile(this.Services.FileSystem, "test.txt");

      var doc = new XmlTextSnapshot(sourceFile, "<Item><Field Name=\"Text\">123</Field></Item>", string.Empty, string.Empty);
      var root = doc.Root;

      var fields = doc.GetNestedTextNode(root, "Fields");
      Assert.IsNotNull(fields);

      var field = fields.ChildNodes[0];
      Assert.IsNotNull(field);
      Assert.AreEqual("Text", field.GetAttributeValue("Name"));
    }
  }
}

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace FeedCenter.Xml;

public static class XmlExtensions
{
    public static XmlNamespaceManager GetAllNamespaces(this XmlDocument document)
    {
        // Create the namespace manager from the name table
        var namespaceManager = new XmlNamespaceManager(document.NameTable);

        // Create a dictionary of all namespaces
        var allNamespaces = new Dictionary<string, string>();

        // Create an XPathDocument from the XML of the XmlDocument
        var xPathDocument = new XPathDocument(new StringReader(document.InnerXml));

        // Create an XPathNavigator for the document
        var xPathNavigator = xPathDocument.CreateNavigator();

        if (xPathNavigator == null) return namespaceManager;

        // Loop over all elements
        while (xPathNavigator.MoveToFollowing(XPathNodeType.Element))
        {
            // Get the list of local namespaces
            var localNamespaces = xPathNavigator.GetNamespacesInScope(XmlNamespaceScope.Local);

            if (localNamespaces == null) continue;

            // Add all local namespaces to the master list
            foreach (var ns in localNamespaces)
                allNamespaces[ns.Key] = ns.Value;
        }

        // Loop over all namespaces
        foreach (var ns in allNamespaces)
        {
            // Use the key as the name
            var namespaceName = ns.Key;

            // If the name is blank then use "default" instead
            if (string.IsNullOrEmpty(namespaceName))
                namespaceName = "default";

            // Add the namespace to the manager
            namespaceManager.AddNamespace(namespaceName, ns.Value);
        }

        // Add the default namespace if missing
        if (!namespaceManager.HasNamespace("default"))
            namespaceManager.AddNamespace("default", "");

        return namespaceManager;
    }
}
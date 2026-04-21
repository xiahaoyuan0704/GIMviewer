using System.Collections.Generic;
using Battlehub.UIControls;
using UnityEngine;
using UnityEngine.UI;

namespace NewGimApp
{
    /// <summary>
    /// Minimal tree + property panel binder for NewGimApp.
    /// Hook this to existing Battlehub TreeView and a key-value row prefab.
    /// </summary>
    public class GimAppTreeViewController : MonoBehaviour
    {
        public GimAppDemoController demoController;
        public TreeView treeView;
        public GameObject propertiesContent;
        public GameObject propertyRowPrefab;

        private readonly List<TreeItem> treeItems = new List<TreeItem>();

        private class TreeItem
        {
            public GimNode Node;
            public List<TreeItem> Children = new List<TreeItem>();
        }

        private void Awake()
        {
            if (demoController != null)
            {
                demoController.DocumentParsed += OnDocumentParsed;
            }

            if (treeView != null)
            {
                treeView.ItemDataBinding += OnItemDataBinding;
                treeView.ItemExpanding += OnItemExpanding;
                treeView.SelectionChanged += OnSelectionChanged;
            }
        }

        private void OnDestroy()
        {
            if (demoController != null)
            {
                demoController.DocumentParsed -= OnDocumentParsed;
            }

            if (treeView != null)
            {
                treeView.ItemDataBinding -= OnItemDataBinding;
                treeView.ItemExpanding -= OnItemExpanding;
                treeView.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnDocumentParsed(GimDocument document)
        {
            treeItems.Clear();
            if (document != null && document.Root != null)
            {
                treeItems.Add(BuildTreeItem(document.Root));
            }

            if (treeView != null)
            {
                treeView.Items = treeItems;
            }

            ClearPropertyRows();
        }

        private TreeItem BuildTreeItem(GimNode node)
        {
            var item = new TreeItem { Node = node };
            for (int i = 0; i < node.Children.Count; i++)
            {
                item.Children.Add(BuildTreeItem(node.Children[i]));
            }
            return item;
        }

        private void OnItemDataBinding(object sender, TreeViewItemDataBindingArgs e)
        {
            var item = e.Item as TreeItem;
            if (item == null)
            {
                return;
            }

            var text = e.ItemPresenter.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                if (item.Node != null)
                {
                    text.text = string.Format("[{0}] {1}", item.Node.NodeType, item.Node.Name);
                }
                else
                {
                    text.text = "<null>";
                }
            }

            e.HasChildren = item.Children.Count > 0;
        }

        private void OnItemExpanding(object sender, ItemExpandingArgs e)
        {
            var item = e.Item as TreeItem;
            if (item != null)
            {
                e.Children = item.Children;
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs e)
        {
            var item = e.NewItem as TreeItem;
            RenderProperties(item != null ? item.Node : null);
        }

        private void RenderProperties(GimNode node)
        {
            ClearPropertyRows();
            if (node == null || node.Properties == null || propertyRowPrefab == null || propertiesContent == null)
            {
                return;
            }

            foreach (var kv in node.Properties)
            {
                var row = Instantiate(propertyRowPrefab, propertiesContent.transform);
                row.transform.localScale = Vector3.one;
                var keyNode = row.transform.Find("key");
                var valueNode = row.transform.Find("value");
                if (keyNode != null)
                {
                    var keyText = keyNode.GetComponent<Text>();
                    if (keyText != null)
                    {
                        keyText.text = kv.Key;
                    }
                }
                if (valueNode != null)
                {
                    var valueText = valueNode.GetComponent<Text>();
                    if (valueText != null)
                    {
                        valueText.text = kv.Value;
                    }
                }
            }
        }

        private void ClearPropertyRows()
        {
            if (propertiesContent == null)
            {
                return;
            }

            for (int i = propertiesContent.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(propertiesContent.transform.GetChild(i).gameObject);
            }
        }
    }
}

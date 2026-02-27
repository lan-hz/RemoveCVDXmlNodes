using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using System.Xml;

namespace RemoveCVDXmlNodes
{
    public partial class MainForm : Form
    {
        private Button selectButton;

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            selectButton = new Button
            {
                Text = "选择 CVD 文件",
                Size = new Size(120, 40),
                Location = new Point(50, 50)
            };
            selectButton.Click += SelectButton_Click;

            this.ClientSize = new Size(250, 150);
            this.Controls.Add(selectButton);
            this.Text = "XML 处理器";
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CVD 文件|*.cvd|所有文件|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ProcessZipFile(openFileDialog.FileName);
                }
            }
        }

        private void ProcessZipFile(string zipFilePath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // 解压 ZIP 文件
                ZipFile.ExtractToDirectory(zipFilePath, tempDir);

                string xmlFilePath = Path.Combine(tempDir, "doc", "equips.xml");
                if (!File.Exists(xmlFilePath))
                {
                    MessageBox.Show("Cvd文件中未找到 doc\\equips.xml 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 创建 XmlDocument 实例
                XmlDocument xmlDoc = new XmlDocument();
                // 加载 XML 文件
                xmlDoc.Load(xmlFilePath);

                // 定义要删除的标签列表
                string[] targetTags = { "itemSource", "cargoAssetRef", "StorePercent","itemTypeString","assetRef", "asset"};

                // 从根节点开始递归删除目标标签
                if (xmlDoc.DocumentElement != null)
                {
                    RemoveTargetTags(xmlDoc.DocumentElement, targetTags);
                }

                // 保存修改后的 XML 文件
                xmlDoc.Save(xmlFilePath);

                // 重新压缩文件
                string newFileName = $"{Path.GetFileNameWithoutExtension(zipFilePath)}_set.cvd";
                string newZipFilePath = Path.Combine(Path.GetDirectoryName(zipFilePath), newFileName);
                ZipFile.CreateFromDirectory(tempDir, newZipFilePath);

                MessageBox.Show($"处理完成，已保存到：{newZipFilePath}", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (XmlException ex)
            {
                MessageBox.Show("解析 XML 时出错，请确认文件格式是否完整。\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 删除临时目录
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 递归删除指定标签的节点
        /// </summary>
        /// <param name="element">当前处理的 XmlElement</param>
        /// <param name="tags">要删除的标签列表</param>
        static void RemoveTargetTags(XmlElement element, string[] tags)
        {
            // 遍历当前节点下的所有子节点
            for (int i = element.ChildNodes.Count - 1; i >= 0; i--)
            {
                XmlNode child = element.ChildNodes[i];
                if (child.NodeType == XmlNodeType.Element)
                {
                    XmlElement childElement = (XmlElement)child;
                    if (Array.IndexOf(tags, childElement.Name) != -1)
                    {
                        element.RemoveChild(child);
                    }
                    else
                    {
                        RemoveTargetTags(childElement, tags);
                    }
                }
            }
        }
    }
}
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Generic;

namespace RemoveCVDXmlNodes
{
    public partial class MainForm : Form
    {
        private Button selectButton;
        // 默认要删除的标签列表（用于重置）
        readonly string[] defaultTags = { "itemSource", "cargoAssetRef", "StorePercent", "itemTypeString", "assetRef", "asset" };
        // 可变的标签列表
        List<string> targetTagsList;
        string filePath;

        // 界面控件
        private Label filePathLabel;
        private ListBox tagsListBox;
        private TextBox newTagTextBox;
        private Button addTagButton;
        private Button removeTagButton;
        private Button resetButton;
        private Button clearButton;
        private Button executeButton;

        public MainForm()
        {
            InitializeComponents();
        }

        private void AddTagButton_Click(object sender, EventArgs e)
        {
            string newTag = newTagTextBox.Text.Trim();
            if (string.IsNullOrEmpty(newTag))
            {
                MessageBox.Show("请输入标签名。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!targetTagsList.Contains(newTag))
            {
                targetTagsList.Add(newTag);
                tagsListBox.Items.Add(newTag);
                newTagTextBox.Clear();
            }
        }

        private void RemoveTagButton_Click(object sender, EventArgs e)
        {
            if (tagsListBox.SelectedItem == null)
            {
                MessageBox.Show("请先选择要删除的标签。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string tag = tagsListBox.SelectedItem.ToString();
            targetTagsList.Remove(tag);
            tagsListBox.Items.Remove(tag);
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            targetTagsList.Clear();
            tagsListBox.Items.Clear();
            foreach (var t in defaultTags)
            {
                targetTagsList.Add(t);
                tagsListBox.Items.Add(t);
            }
        }
        private void ClearButton_Click(object sender, EventArgs e)
        {
            targetTagsList.Clear();
            tagsListBox.Items.Clear();
        }

        private void ExecuteButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("请先选择有效的 CVD 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (targetTagsList.Count == 0)
            {
                MessageBox.Show("标签列表为空，请添加至少一个标签。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 禁用按钮以防重复点击
            executeButton.Enabled = false;
            try
            {
                ProcessZipFile(filePath);
            }
            finally
            {
                executeButton.Enabled = true;
            }
        }

        private void InitializeComponents()
        {
            // 初始化默认标签列表
            targetTagsList = new List<string>(defaultTags);

            this.ClientSize = new Size(420, 320);
            this.Text = "XML 处理器";

            // 文件路径标签，位于选择按钮左侧
            filePathLabel = new Label
            {
                Text = "未选择文件",
                AutoSize = false,
                Size = new Size(260, 24),
                Location = new Point(20, 16)
            };

            selectButton = new Button
            {
                Text = "选择 CVD 文件",
                Size = new Size(120, 28),
                Location = new Point(this.ClientSize.Width - 140, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            selectButton.Click += SelectButton_Click;

            // 标签列表
            tagsListBox = new ListBox
            {
                Location = new Point(20, 60),
                Size = new Size(360, 140)
            };
            // 填充初始标签
            foreach (var t in targetTagsList)
                tagsListBox.Items.Add(t);

            // 新标签输入框和添加/删除按钮
            newTagTextBox = new TextBox
            {
                Location = new Point(20, 210),
                Size = new Size(200, 24)
            };

            addTagButton = new Button
            {
                Text = "添加",
                Location = new Point(230, 208),
                Size = new Size(70, 26)
            };
            addTagButton.Click += AddTagButton_Click;

            removeTagButton = new Button
            {
                Text = "删除",
                Location = new Point(310, 208),
                Size = new Size(70, 26)
            };
            removeTagButton.Click += RemoveTagButton_Click;

            // 重置和执行按钮
            resetButton = new Button
            {
                Text = "重置",
                Location = new Point(20, 250),
                Size = new Size(80, 30)
            };
            resetButton.Click += ResetButton_Click;

            clearButton = new Button
            {
                Text = "清除",
                Location = new Point(120, 250),
                Size = new Size(80, 30)
            };
            clearButton.Click += ClearButton_Click;
            executeButton = new Button
            {
                Text = "执行",
                Location = new Point(220, 250),
                Size = new Size(80, 30)
            };
            executeButton.Click += ExecuteButton_Click;

            this.Controls.Add(filePathLabel);
            this.Controls.Add(selectButton);
            this.Controls.Add(tagsListBox);
            this.Controls.Add(newTagTextBox);
            this.Controls.Add(addTagButton);
            this.Controls.Add(removeTagButton);
            this.Controls.Add(resetButton);
            this.Controls.Add(clearButton);
            this.Controls.Add(executeButton);
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CVD 文件|*.cvd|所有文件|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                    // 显示已选择的文件路径（简短显示）
                    filePathLabel.Text = filePath;
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

              

                // 从根节点开始递归删除目标标签
                if (xmlDoc.DocumentElement != null)
                {
                    RemoveTargetTags(xmlDoc.DocumentElement, targetTagsList.ToArray());
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
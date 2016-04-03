﻿// SQL Notebook
// Copyright (C) 2016 Brian Luft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace SqlNotebook {
    public partial class ExplorerControl : UserControl {
        private readonly NotebookManager _manager;
        private readonly ImageList _paddedImageList;
        private readonly IWin32Window _mainForm;

        public ExplorerControl(NotebookManager manager, IWin32Window mainForm) {
            InitializeComponent();
            _mainForm = mainForm;
            _manager = manager;
            _manager.NotebookChange += (sender, e) => HandleNotebookChange(e);

            _paddedImageList = new ImageList {
                ImageSize = new Size(25, 17),
                ColorDepth = ColorDepth.Depth32Bit
            };
            foreach (Image image in _imageList.Images) {
                var newImage = new Bitmap(25, 17, image.PixelFormat);
                using (var g = Graphics.FromImage(newImage)) {
                    g.DrawImage(image, 7, 1);
                }
                _paddedImageList.Images.Add(newImage);
            }
            _list.SmallImageList = _paddedImageList;
        }

        private void HandleNotebookChange(NotebookChangeEventArgs e) {
            BeginInvoke(new MethodInvoker(() => {
                _list.BeginUpdate();
                foreach (var item in e.RemovedItems) {
                    foreach (ListViewItem lvi in _list.Items) {
                        if (lvi.Text == item.Name) {
                            lvi.Remove();
                            break;
                        }
                    }
                }
                foreach (var item in e.AddedItems) {
                    var lvi = _list.Items.Add(item.Name);
                    lvi.Group = _list.Groups[item.Type.ToString()];
                    lvi.ImageIndex = (int)item.Type;
                }
                _list.Sort();
                _list.EndUpdate();
            }));
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            _list.Columns[0].Width = _list.Width - SystemInformation.VerticalScrollBarWidth - 5;
        }

        private void List_ItemActivate(object sender, EventArgs e) {
            if (_list.SelectedItems.Count != 1) {
                return;
            }
            var lvi = _list.SelectedItems[0];
            var type = (NotebookItemType)Enum.Parse(typeof(NotebookItemType), lvi.Group.Name);
            _manager.OpenItem(new NotebookItem(type, lvi.Text));
        }

        private void DeleteMnu_Click(object sender, EventArgs e) {
            if (_list.SelectedItems.Count != 1) {
                return;
            }
            var lvi = _list.SelectedItems[0];
            var name = lvi.Text;

            var d = new TaskDialog {
                Caption = "Delete Item",
                InstructionText = $"Are you sure you want to delete\n\"{name}\"?",
                StartupLocation = TaskDialogStartupLocation.CenterOwner,
                OwnerWindowHandle = _mainForm.Handle,
                Icon = TaskDialogStandardIcon.Warning,
                Cancelable = true
            };
            var deleteBtn = new TaskDialogButton("delete", "&Delete");
            deleteBtn.Click += (sender2, e2) => { d.Close(TaskDialogResult.Ok); };
            var cancelBtn = new TaskDialogButton("cancel", "Cancel");
            cancelBtn.Click += (sender2, e2) => { d.Close(TaskDialogResult.Cancel); };
            d.Controls.Add(deleteBtn);
            d.Controls.Add(cancelBtn);
            using (d) {
                if (d.Show() != TaskDialogResult.Ok) {
                    return;
                }
            }

            var type = (NotebookItemType)Enum.Parse(typeof(NotebookItemType), lvi.Group.Name);
            _manager.CloseItem(new NotebookItem(type, name));

            new WaitForm("Delete", "Deleting the selected item...", () => {
                _manager.Notebook.Invoke(() => {
                    switch (type) {
                        case NotebookItemType.Console:
                        case NotebookItemType.Script:
                        case NotebookItemType.Note:
                            _manager.Notebook.Execute("DELETE FROM sqlnotebook_items WHERE name = @name", 
                                new Dictionary<string, object> { ["@name"] = name });
                            break;

                        case NotebookItemType.Table:
                            _manager.Notebook.Execute($"DROP TABLE \"{name.Replace("\"", "\"\"")}\"");
                            break;

                        case NotebookItemType.View:
                            _manager.Notebook.Execute($"DROP VIEW \"{name.Replace("\"", "\"\"")}\"");
                            break;
                    }
                });
            }).ShowDialog(this, 25);

            _manager.Rescan();
        }

        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e) {
            var isSelection = _list.SelectedItems.Count == 1;
            _deleteMnu.Enabled = isSelection;
        }
    }
}
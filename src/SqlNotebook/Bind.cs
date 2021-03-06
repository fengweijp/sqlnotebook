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
using System.Linq;
using System.Windows.Forms;
using SqlNotebookScript.Utils;

namespace SqlNotebook {
    public static class Bind {
        public static void OnChange(IEnumerable<Slot> slots, EventHandler handler) {
            foreach (var slot in slots) {
                slot.ChangeNoData += () => handler?.Invoke(slot, EventArgs.Empty);
            }
        }

        public static void BindAny(IReadOnlyList<Slot<bool>> slots, Action<bool> handler) {
            Action wrapper = () => handler(slots.Any(x => x.Value));
            foreach (var slot in slots) {
                slot.ChangeNoData += wrapper;
            }
        }
        
        public static void BindValue(this NumericUpDown self, Slot<int> slot) {
            slot.Value = (int)self.Value;
            slot.Change += (old, val) => {
                if (self.Value != val) {
                    self.Value = val;
                }
            };
            self.ValueChanged += (sender, e) => {
                slot.Value = (int)self.Value;
            };
        }

        public static void BindChecked(this CheckBox self, Slot<bool> slot) {
            slot.Value = self.Checked;
            slot.Change += (old, val) => {
                if (self.Checked != val) {
                    self.Checked = val;
                }
            };
            self.CheckedChanged += (sender, e) => {
                slot.Value = self.Checked;
            };
        }

        public static void BindSelectedValue<T>(this ComboBox self, Slot<T> slot) {
            slot.Value = (T)self.SelectedValue;
            slot.Change += (old, val) => {
                var ctlVal = self.SelectedValue;
                if ((ctlVal == null && val != null) || (ctlVal != null && !ctlVal.Equals(val))) {
                    self.SelectedValue = val;
                }
            };
            self.SelectedValueChanged += (sender, e) => {
                slot.Value = (T)self.SelectedValue;
            };
        }

        public static void BindText(this ComboBox self, Slot<string> slot) {
            slot.Value = self.Text;
            slot.Change += (old, val) => {
                if (self.Text != val) {
                    self.Text = val;
                }
            };
            self.TextChanged += (sender, e) => {
                slot.Value = self.Text;
            };
        }
    }
}

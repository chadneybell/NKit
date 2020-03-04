using System.Collections.Generic;
using System.Windows.Forms;

namespace Nanook.NKit
{
    public partial class MasksForm : Form
    {
        public MasksForm()
        {
            InitializeComponent();
        }

        public DialogResult ShowDialogWithInitialise(IWin32Window owner, Dictionary<string, string> masks)
        {
            foreach (KeyValuePair<string, string> mask in masks)
            {
                string[] names = mask.Key.Split(':');
                ListViewItem li = new ListViewItem(names[1])
                {
                    Group = lvw.Groups["grp" + names[0]]
                };
                li.SubItems.Add(mask.Value);
                lvw.Items.Add(li);
            }
            return base.ShowDialog(owner);
        }
    }
}

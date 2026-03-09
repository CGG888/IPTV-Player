using System.Windows;

namespace LibmpvIptvClient.Controls
{
    public partial class DeleteRecordingDialog : Window
    {
        public string Choice { get; private set; } = "";
        public DeleteRecordingDialog()
        {
            InitializeComponent();
            try { Title = Helpers.ResxLocalizer.Get("Dialog_Delete_Title", "删除录播"); } catch { Title = "删除录播"; }
            try { TxtTitle.Text = Helpers.ResxLocalizer.Get("Dialog_Delete_Title", "删除录播"); } catch { TxtTitle.Text = "删除录播"; }
            try { TxtDesc.Text = Helpers.ResxLocalizer.Get("Dialog_Delete_Desc", "选择要删除的对象"); } catch { TxtDesc.Text = "选择要删除的对象"; }
            try { BtnDeleteLocal.Content = Helpers.ResxLocalizer.Get("Dialog_Delete_Local", "仅删除本地"); } catch { BtnDeleteLocal.Content = "仅删除本地"; }
            try { BtnDeleteRemote.Content = Helpers.ResxLocalizer.Get("Dialog_Delete_Remote", "仅删除网络"); } catch { BtnDeleteRemote.Content = "仅删除网络"; }
            try { BtnDeleteBoth.Content = Helpers.ResxLocalizer.Get("Dialog_Delete_Both", "全部删除"); } catch { BtnDeleteBoth.Content = "全部删除"; }
            BtnDeleteLocal.Click += (s, e) => { Choice = "local"; DialogResult = true; Close(); };
            BtnDeleteRemote.Click += (s, e) => { Choice = "remote"; DialogResult = true; Close(); };
            BtnDeleteBoth.Click += (s, e) => { Choice = "both"; DialogResult = true; Close(); };
        }
    }
}


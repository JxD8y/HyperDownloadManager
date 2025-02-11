using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HyperDownloadManager.Dialogs.MessageBoxDialog;

namespace HyperDownloadManager.Dialogs
{
    public class DialogViewModel
    {
        public string? Title { get; set; }
        public DialogMode DialogMode { get; set; } = DialogMode.InApp;
        public Page? DialogContent { get; set; }
        public float? DialogHeight { get; set; }
        public float? DialogWidth { get; set; }
        public bool? OnTopDialog { get; set; }
        public DialogBox? Dialog { get; set; }
        public MessageBoxStatus messageBoxStatus { get; set; } = MessageBoxStatus.NONE;
        public DialogViewModel() { }
        public DialogViewModel(string title, Page dialogContent)
        {
            Title = title;
            DialogContent = dialogContent;
        }
        public DialogViewModel(Page dialogContent, DialogMode dm)
        {
            Title = "";
            DialogContent = dialogContent;
            DialogMode = dm;
        }
        public DialogViewModel(string title, Page dialogContent, float dialogHeight, float dialogWidth, bool onTopDialog)
        {
            Title = title;
            DialogContent = dialogContent;
            DialogHeight = dialogHeight;
            DialogWidth = dialogWidth;
            OnTopDialog = onTopDialog;
        }
        public async Task<MessageBoxStatus?> Show()
        {

            if (DialogContent != null)
            {
                if (DialogMode == DialogMode.Window)
                {
                    GlobalSupervisor.MainWindow.Dispatcher.Invoke(() =>
                    {
                        if (Title == null)
                            Title = "Message";
                        DialogBox dbx = new DialogBox(Title);
                        dbx.Width = DialogContent.MaxWidth + 5;
                        dbx.Height = DialogContent.MaxHeight + 10;
                        this.Dialog = dbx;
                        dbx.Mainframe.Content = DialogContent;
                        dbx.ShowDialog();
                    });
                }
                else
                {
                    GlobalSupervisor.MainWindow.Dispatcher.Invoke(() =>
                    {
                        GlobalSupervisor.MainWindow.ShowDialog(DialogContent);
                    });
                    while (this.messageBoxStatus == MessageBoxStatus.NONE)
                    {
                        await Task.Delay(1000);
                    }
                }
            }

            return this.messageBoxStatus;
        }
        public void Close(MessageBoxStatus state = MessageBoxStatus.OK)
        {
            if (DialogMode == DialogMode.Window)
            {
                this.Dialog?.Close();
            }
            else
            {
                GlobalSupervisor.MainWindow.CloseDialog();
            }
            this.messageBoxStatus = state;
        }
    
    }
}

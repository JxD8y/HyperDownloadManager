using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using HyperDownloadManager.Dialogs.MessageBoxDialog;

namespace HyperDownloadManager.Dialogs
{
    public static class DialogManager
    {
        public static List<DialogViewModel> Dialogs = new List<DialogViewModel>();
        public static DialogViewModel? CurrentDialog { get; set; } = null;
        public async static void ShowDialog(string title, Page dialogContent, DialogMode dialogMode = DialogMode.InApp)
        {
            DialogViewModel dialogViewModel = new DialogViewModel(dialogContent, dialogMode);
            Dialogs.Add(dialogViewModel);
            CurrentDialog = dialogViewModel;
            await dialogViewModel.Show();
        }
        public static void Close()
        {
            CurrentDialog?.Close();
        }
        public static bool IsDialogPresent()
        {
            if (Dialogs.Count == 0)
            {
                return false;
            }
            return true;
        }
        public async static Task<MessageBoxStatus?> ShowMessageBox(string message, MessageLevel level, ButtonOrder buttonOrder, bool NewWindow = false)
        {
            DialogViewModel dialog = new DialogViewModel();
            MessageBoxDialogView box;
            if (GlobalSupervisor.MainWindow != null)
            {
                GlobalSupervisor.MainWindow.Dispatcher.Invoke(() =>
                {
                    if (NewWindow)
                        dialog.DialogMode = DialogMode.Window;
                    else
                        dialog.DialogMode = DialogMode.InApp;

                    box = new MessageBoxDialogView(message, level, buttonOrder, dialog);
                    dialog.DialogContent = box;

                });
            }
            else
            {
                System.Windows.MessageBox.Show(message);
            }
            return await dialog.Show();
        }
    }
}

using ChrisKaczor.Wpf.Validation;
using System;
using System.Linq;
using System.Windows;
using FeedCenter.Accounts;

namespace FeedCenter.Options;

public partial class AccountWindow
{
    private Account _account;
    private bool _isNew;
    private readonly FeedCenterEntities _entities;

    public AccountWindow(FeedCenterEntities entities)
    {
        _entities = entities;

        InitializeComponent();
    }

    public bool? Display(Account account, Window owner, bool isNew)
    {
        _account = account;
        _isNew = isNew;

        DataContext = account;

        Title = isNew ? Properties.Resources.AccountWindowAdd : Properties.Resources.AccountWindowEdit;

        Owner = owner;

        return ShowDialog();
    }

    private async void HandleOkayButtonClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var transaction = _entities.BeginTransaction();

            if (!this.IsValid(OptionsTabControl))
            {
                transaction.Rollback();
                return;
            }

            if (_isNew)
            {
                _entities.Accounts.Add(_account);
            }

            await transaction.CommitAsync();

            var accountId = _account.Id;

            var accountReadInput = new AccountReadInput(_entities, null, true, () => AccountReadProgressBar.Value++);

            AccountReadProgressBar.Value = 0;
            AccountReadProgressBar.Maximum = await _account.GetProgressSteps(accountReadInput);

            AccountReadProgress.Visibility = Visibility.Visible;
            ButtonPanel.Visibility = Visibility.Collapsed;

            //var entities = new FeedCenterEntities();
            var account = _entities.Accounts.First(a => a.Id == accountId);
            await account.Read(accountReadInput);

            DialogResult = true;

            Close();
        }
        catch (Exception exception)
        {
            MainWindow.HandleException(exception);
        }
    }
}
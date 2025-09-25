using ChrisKaczor.Wpf.Validation;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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

    private void HandleOkayButtonClick(object sender, RoutedEventArgs e)
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

        transaction.Commit();

        var accountId = _account.Id;

        AccountReadProgressBar.Value = 0;
        AccountReadProgressBar.Maximum = _account.GetProgressSteps(_entities) + 1;

        AccountReadProgress.Visibility = Visibility.Visible;
        ButtonPanel.Visibility = Visibility.Collapsed;

        var dispatcher = Dispatcher.CurrentDispatcher;

        Task.Run(() =>
        {
            var entities = new FeedCenterEntities();
            var account = entities.Accounts.First(a => a.Id == accountId);
            var accountReadInput = new AccountReadInput(entities, null, true, () => dispatcher.Invoke(() => AccountReadProgressBar.Value++));
            account.Read(accountReadInput);

            dispatcher.Invoke(() =>
            {
                DialogResult = true;

                Close();
            });
        });
    }
}
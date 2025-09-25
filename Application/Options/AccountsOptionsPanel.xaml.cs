using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FeedCenter.Options;

public partial class AccountsOptionsPanel
{
    private readonly FeedCenterEntities _entities;

    public AccountsOptionsPanel(Window parentWindow, FeedCenterEntities entities) : base(parentWindow, entities)
    {
        _entities = entities;

        InitializeComponent();
    }

    public override string CategoryName => Properties.Resources.optionCategoryAccounts;

    public override void LoadPanel()
    {
        base.LoadPanel();

        var collectionViewSource = new CollectionViewSource { Source = _entities.Accounts };
        collectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        collectionViewSource.IsLiveSortingRequested = true;
        collectionViewSource.View.Filter = item =>
        {
            if (item is not Account account)
                return false;

            // Filter out local accounts
            return account.Type != AccountType.Local;
        };

        AccountDataGrid.ItemsSource = collectionViewSource.View;
        AccountDataGrid.SelectedIndex = 0;

        SetAccountButtonStates();
    }

    private void SetAccountButtonStates()
    {
        AddAccountButton.IsEnabled = true;

        EditAccountButton.IsEnabled = AccountDataGrid.SelectedItem != null;
        DeleteAccountButton.IsEnabled = AccountDataGrid.SelectedItem != null;
    }

    private void AddAccount()
    {
        var account = new Account(AccountType.Fever);

        var accountWindow = new AccountWindow(_entities);

        var result = accountWindow.Display(account, Window.GetWindow(this), true);

        if (!result.HasValue || !result.Value)
            return;

        AccountDataGrid.SelectedItem = account;

        SetAccountButtonStates();
    }

    private void EditSelectedAccount()
    {
        if (AccountDataGrid.SelectedItem == null)
            return;

        var account = (Account) AccountDataGrid.SelectedItem;

        var accountWindow = new AccountWindow(_entities);

        accountWindow.Display(account, Window.GetWindow(this), false);
    }

    private void DeleteSelectedAccount()
    {
        var account = (Account) AccountDataGrid.SelectedItem;

        if (MessageBox.Show(ParentWindow, string.Format(Properties.Resources.ConfirmDeleteAccount, account.Name),
                Properties.Resources.ConfirmDeleteTitle, MessageBoxButton.YesNo, MessageBoxImage.Question,
                MessageBoxResult.No) == MessageBoxResult.No)
            return;

        var index = AccountDataGrid.SelectedIndex;

        if (index == AccountDataGrid.Items.Count - 1)
            AccountDataGrid.SelectedIndex = index - 1;
        else
            AccountDataGrid.SelectedIndex = index + 1;

        _entities.SaveChanges(() => _entities.Accounts.Remove(account));

        SetAccountButtonStates();
    }

    private void HandleAddAccountButtonClick(object sender, RoutedEventArgs e)
    {
        AddAccount();
    }

    private void HandleEditAccountButtonClick(object sender, RoutedEventArgs e)
    {
        EditSelectedAccount();
    }

    private void HandleDeleteAccountButtonClick(object sender, RoutedEventArgs e)
    {
        DeleteSelectedAccount();
    }

    private void HandleAccountDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SetAccountButtonStates();
    }

    private void HandleAccountDataGridRowMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!EditAccountButton.IsEnabled)
            return;

        EditSelectedAccount();
    }
}
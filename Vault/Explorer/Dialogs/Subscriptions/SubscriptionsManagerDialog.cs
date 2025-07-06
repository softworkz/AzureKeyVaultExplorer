// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

using Microsoft.Azure;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Identity.Client;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Vault.Library;

namespace Microsoft.Vault.Explorer
{
    public partial class SubscriptionsManagerDialog : Form
    {
        const string ApiVersion = "api-version=2016-07-01";
        const string ManagmentEndpoint = "https://management.azure.com/";
        const string AddAccountText = "Add New Account";
        const string AddDomainHintText = "How to add new domain hint here...";
        const string AddDomainHintInstructions = @"To add new domain hint, just follow below steps:
1) In the main window open Settings dialog
2) Add domain hint line to 'Domain hints' property
3) Click on 'OK' button to save and close Settings dialog
4) Open Subscriptions Manager dialog";

        private AccountItem _currentAccountItem;
        private AuthenticationResult _currentAuthResult;
        private KeyVaultManagementClient _currentKeyVaultMgmtClient;
        private readonly HttpClient _httpClient;
        private int _initialVaultCount = 0;

        public VaultAlias CurrentVaultAlias { get; private set; }

        public SubscriptionsManagerDialog()
        {
            InitializeComponent();
            _httpClient = new HttpClient();

            // Create Default accounts based on domain hints and aliases.
            bool hasPreConfiguredAccounts = false;
            foreach (string userAccountName in Settings.Default.UserAccountNamesList)
            {
                string[] accounts = userAccountName.Split('@');
                if (accounts.Length < 2)
                {
                    continue;
                }

                uxComboBoxAccounts.Items.Add(new AccountItem(accounts[1], accounts[0]));
                hasPreConfiguredAccounts = true;
            }
            
            uxComboBoxAccounts.Items.Add(AddAccountText);
            uxComboBoxAccounts.Items.Add(AddDomainHintText);
            
            // Only auto-select if we have pre-configured accounts, otherwise let user choose
            if (hasPreConfiguredAccounts)
            {
                uxComboBoxAccounts.SelectedIndex = 0;
            }
            else
            {
                // No pre-configured accounts, don't auto-select anything
                uxComboBoxAccounts.SelectedIndex = -1;
                uxComboBoxAccounts.Text = "Select an account or add new...";
            }
        }

        private UxOperation NewUxOperationWithProgress(params ToolStripItem[] controlsToToggle) => new UxOperation(null, uxStatusLabel, uxProgressBar, uxButtonCancelOperation, controlsToToggle);

        private async void uxComboBoxAccounts_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(uxComboBoxAccounts.SelectedItem)
            {
                case null:
                    return;
                    
                case AddAccountText:
                    AddNewAccount();
                    break;

                case AddDomainHintText:
                    // Display instructions on how to add domain hint
                    MessageBox.Show(AddDomainHintInstructions, Utils.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    uxComboBoxAccounts.SelectedItem = null;
                    return;

                case AccountItem account:
                    // Authenticate into selected account
                    _currentAccountItem = account;
                    await GetAuthenticationTokenAsync();
                    if (_currentAuthResult.Account != null)
                    {
                        _currentAccountItem.UserAlias = _currentAuthResult.Account.Username.Split('@')[0];
                    }
                    break;

                default:
                    return;
            }

            if (_currentAuthResult == null)
            {
                return;
            }

            using (var op = NewUxOperationWithProgress(uxComboBoxAccounts))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _currentAuthResult.AccessToken);
                var hrm = await _httpClient.GetAsync($"{ManagmentEndpoint}subscriptions?{ApiVersion}", op.CancellationToken);
                var json = await hrm.Content.ReadAsStringAsync();
                var subs = JsonConvert.DeserializeObject<SubscriptionsResponse>(json);

                uxListViewSubscriptions.Items.Clear();
                uxListViewVaults.Items.Clear();
                uxPropertyGridVault.SelectedObject = null;
                foreach (var s in subs.Subscriptions)
                {
                    uxListViewSubscriptions.Items.Add(new ListViewItemSubscription(s));
                }
            }
        }

        private async void uxListViewSubscriptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListViewItemSubscription s = uxListViewSubscriptions.SelectedItems.Count > 0 ? (ListViewItemSubscription)uxListViewSubscriptions.SelectedItems[0] : null;
            if (null == s) return;
            using (var op = NewUxOperationWithProgress(uxComboBoxAccounts))
            {
                var tvcc = new TokenCredentials(_currentAuthResult.AccessToken);
                _currentKeyVaultMgmtClient = new KeyVaultManagementClient(tvcc) { SubscriptionId = s.Subscription.SubscriptionId.ToString() };
                var vaults = await _currentKeyVaultMgmtClient.Vaults.ListAsync(null, op.CancellationToken);
                uxListViewVaults.Items.Clear();
                foreach (var v in vaults)
                {
                    uxListViewVaults.Items.Add(new ListViewItemVault(v));
                }
            }
        }

        private async void uxListViewVaults_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListViewItemSubscription s = uxListViewSubscriptions.SelectedItems.Count > 0 ? (ListViewItemSubscription)uxListViewSubscriptions.SelectedItems[0] : null;
            ListViewItemVault v = uxListViewVaults.SelectedItems.Count > 0 ? (ListViewItemVault)uxListViewVaults.SelectedItems[0] : null;
            uxButtonOK.Enabled = false;
            if ((null == s) || (null == v)) return;
            using (var op = NewUxOperationWithProgress(uxComboBoxAccounts))
            {
                var vault = await _currentKeyVaultMgmtClient.Vaults.GetAsync(v.GroupName, v.Name);
                uxPropertyGridVault.SelectedObject = new PropertyObjectVault(s.Subscription, v.GroupName, vault);
                uxButtonOK.Enabled = true;
                
                CurrentVaultAlias = new VaultAlias(v.Name, new string[] { v.Name }, new string[] { "Custom" }) 
                { 
                    DomainHint = _currentAccountItem.DomainHint, 
                    UserAlias = _currentAccountItem.UserAlias,
                    IsNew = true  // Mark as new since it's being added from SubscriptionsManagerDialog
                };
            }
        }


        private async void AddNewAccount()
        {
            try
            {
                // For new account, use "common" as domain hint to let Azure AD determine the tenant
                _currentAccountItem = new AccountItem("common", null);
                await GetAuthenticationTokenAsync();

                // Get new user account and add it to default settings
                string userAccountName = _currentAuthResult.Account?.Username ?? "unknown@unknown.com";
                string[] userLogin = userAccountName.Split('@');
                _currentAccountItem.UserAlias = userLogin[0];
                _currentAccountItem.DomainHint = userLogin[1];
                Settings.Default.AddUserAccountName(userAccountName);

                // Add the new account to the dropdown and select it
                var newAccountItem = new AccountItem(_currentAccountItem.DomainHint, _currentAccountItem.UserAlias);
                uxComboBoxAccounts.Items.Insert(0, newAccountItem);
                uxComboBoxAccounts.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Authentication failed: {ex.Message}", "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Reset selection to allow user to try again
                uxComboBoxAccounts.SelectedIndex = -1;
                uxComboBoxAccounts.Text = "Select an account or add new...";
            }
        }

        // Attempt to authenticate with current account.
        private async Task GetAuthenticationTokenAsync()
        {
            VaultAccessUserInteractive vaui = new VaultAccessUserInteractive(_currentAccountItem.DomainHint, _currentAccountItem.UserAlias);
            string[] scopes = VaultAccess.ConvertResourceToScopes(ManagmentEndpoint);
            _currentAuthResult = await vaui.AcquireTokenAsync(scopes, _currentAccountItem.UserAlias);
        }
    }

    #region Aux UI related classes

    #endregion

    #region Managment endpoint JSON response classes

    #endregion
}

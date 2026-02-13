using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using InvoiceApp.Models;
using InvoiceApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace InvoiceApp.ViewModels
{
    public class StatementListViewModel : ViewModelBase
    {
        private readonly IStatementService _statementService;
        private readonly IClientService _clientService;
        private ObservableCollection<Statement> _statements = new();
        private Statement? _selectedStatement;
        private readonly IPdfService _pdfService;
        private readonly IEmailService _emailService;
        private readonly ISettingsService _settingsService;

        public StatementListViewModel(
        IStatementService statementService,
        IClientService clientService,
        IPdfService pdfService,
        IEmailService emailService,
        ISettingsService settingsService)
        {
            _statementService = statementService;
            _clientService = clientService;
            _pdfService = pdfService;
            _emailService = emailService;
            _settingsService = settingsService;

            GenerateStatementCommand = new AsyncRelayCommand(GenerateStatementAsync);
            ViewStatementCommand = new RelayCommand(ViewStatement, () => SelectedStatement != null);
            DeleteStatementCommand = new AsyncRelayCommand(DeleteStatementAsync, () => SelectedStatement != null);
            RefreshCommand = new AsyncRelayCommand(LoadStatementsAsync);

            LoadStatementsAsync();
        }

        public ObservableCollection<Statement> Statements
        {
            get => _statements;
            set => SetProperty(ref _statements, value);
        }

        public Statement? SelectedStatement
        {
            get => _selectedStatement;
            set
            {
                if (SetProperty(ref _selectedStatement, value))
                {
                    ((RelayCommand)ViewStatementCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)DeleteStatementCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand GenerateStatementCommand { get; }
        public ICommand ViewStatementCommand { get; }
        public ICommand DeleteStatementCommand { get; }
        public ICommand RefreshCommand { get; }

        private async Task LoadStatementsAsync()
        {
            var statements = await _statementService.GetAllStatementsAsync();
            Statements = new ObservableCollection<Statement>(statements);
        }

        private async Task GenerateStatementAsync()
        {
            var dialog = new Views.StatementGenerateDialog(_statementService, _clientService);
            if (dialog.ShowDialog() == true)
            {
                await LoadStatementsAsync();
            }
        }

        private void ViewStatement()
        {
            if (SelectedStatement != null)
            {
                var window = new Views.StatementViewDialog(_statementService, _pdfService, _emailService, _settingsService, SelectedStatement.Id);
                window.ShowDialog();
            }
        }

        private async Task DeleteStatementAsync()
        {
            if (SelectedStatement != null)
            {
                await _statementService.DeleteStatementAsync(SelectedStatement.Id);
                await LoadStatementsAsync();
            }
        }
    }
}



using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace StockTracker.Views
{
	public partial class VATCalculation : UserControl
	{
		public ObservableCollection<VATRow> VATTransactions { get; set; }
		public ObservableCollection<TransactionDetail> TransactionDetails { get; set; }

		public VATCalculation()
		{
			InitializeComponent();
			VATTransactions = new ObservableCollection<VATRow>();
			TransactionDetails = new ObservableCollection<TransactionDetail>();

			VATDataGrid.ItemsSource = VATTransactions;
			CalculationDetailsGrid.ItemsSource = TransactionDetails;
		}

		private void AddRow_Click(object sender, RoutedEventArgs e)
		{
			var newRow = new VATRow();
			VATTransactions.Add(newRow);

			// Simply select the new row - no UpdateLayout or BeginEdit
			VATDataGrid.SelectedItem = newRow;
			VATDataGrid.ScrollIntoView(newRow);
		}

		private void RemoveRow_Click(object sender, RoutedEventArgs e)
		{
			if (VATDataGrid.SelectedItem is VATRow selectedRow)
			{
				VATTransactions.Remove(selectedRow);
				UpdateSummary();

				if (VATTransactions.Count == 0)
				{
					CalculationDetailsPanel.Visibility = Visibility.Collapsed;
					TransactionDetails.Clear();
				}
			}
		}

		private void LoadExample_Click(object sender, RoutedEventArgs e)
		{
			// Clear existing data
			VATTransactions.Clear();

			// Load Bulk Butcher (Pty) Ltd example data
			// Transaction 1: Farmer John - Not a VAT vendor
			VATTransactions.Add(new VATRow
			{
				Description = "Meat purchased from Farmer John (500kg) - Non-VAT vendor",
				TransactionType = "Receivable",
				IsVATApplicable = false, // Farmer John is NOT a VAT vendor
				AmountInclVAT = 115000.00m,
				VATRate = 15
			});

			// Transaction 2: Salaries - No VAT
			VATTransactions.Add(new VATRow
			{
				Description = "Salaries paid for the year",
				TransactionType = "Receivable",
				IsVATApplicable = false, // Salaries are not subject to VAT
				AmountInclVAT = 86250.00m,
				VATRate = 15
			});

			// Transaction 3: Commercial Premises Rental - Landlord is VAT vendor
			VATTransactions.Add(new VATRow
			{
				Description = "Rental for commercial premises - Landlord is VAT vendor",
				TransactionType = "Receivable",
				IsVATApplicable = true, // Landlord IS a VAT vendor
				AmountInclVAT = 92000.00m,
				VATRate = 15
			});

			// Transaction 4: Sales - Bulk Butcher is VAT vendor
			VATTransactions.Add(new VATRow
			{
				Description = "Total sales for the year (meat sold)",
				TransactionType = "Payable",
				IsVATApplicable = true, // Bulk Butcher IS a VAT vendor
				AmountInclVAT = 1150000.00m,
				VATRate = 15
			});

			// Auto-calculate all rows
			foreach (var row in VATTransactions)
			{
				row.RecalculateVAT();
			}

			UpdateSummary();

			MessageBox.Show(
				"Example loaded successfully!\n\n" +
				"Bulk Butcher (Pty) Ltd Scenario:\n" +
				"• Farmer John (Non-VAT vendor): R115,000 - No Input VAT\n" +
				"• Salaries: R86,250 - No VAT applicable\n" +
				"• Rental (VAT vendor landlord): R92,000 incl VAT - Input VAT claimable\n" +
				"• Sales: R1,150,000 incl VAT - Output VAT payable\n\n" +
				"Click 'Calculate VAT' to see the final calculations.\n\n" +
				"Note: The 112kg closing stock does not affect VAT (rule #5).",
				"Example Loaded",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void ClearAll_Click(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show(
				"Are you sure you want to clear all transactions?",
				"Confirm Clear",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (result == MessageBoxResult.Yes)
			{
				VATTransactions.Clear();
				TransactionDetails.Clear();
				CalculationDetailsPanel.Visibility = Visibility.Collapsed;
				UpdateSummary();
			}
		}

		private void Calculate_Click(object sender, RoutedEventArgs e)
		{
			if (VATTransactions.Count == 0)
			{
				MessageBox.Show("Please add at least one transaction row before calculating.",
					"No Data", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			// Calculate VAT for all rows
			foreach (var row in VATTransactions)
			{
				row.RecalculateVAT();
			}
			UpdateSummary();
		}

		private void VATDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (VATDataGrid.SelectedItem is VATRow selectedRow)
			{
				UpdateTransactionDetails(selectedRow);
			}
		}

		private void UpdateTransactionDetails(VATRow row)
		{
			TransactionDetails.Clear();

			if (row == null)
			{
				CalculationDetailsPanel.Visibility = Visibility.Collapsed;
				return;
			}

			CalculationDetailsPanel.Visibility = Visibility.Visible;

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "Description",
				Value = row.Description ?? "N/A"
			});

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "Transaction Type",
				Value = row.TransactionType ?? "Not Set"
			});

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "VAT Applicable",
				Value = row.IsVATApplicable ? "Yes" : "No"
			});

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "Amount (Incl VAT)",
				Value = $"R {row.AmountInclVAT:N2}"
			});

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "VAT Rate",
				Value = row.IsVATApplicable ? $"{row.VATRate:N2}%" : "N/A"
			});

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "Amount (Excl VAT)",
				Value = $"R {row.AmountExclVAT:N2}"
			});

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "VAT Amount",
				Value = $"R {row.VATAmount:N2}"
			});

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "Input VAT",
				Value = $"R {row.InputVAT:N2}"
			});

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "Output VAT",
				Value = $"R {row.OutputVAT:N2}"
			});

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "Net VAT Amount",
				Value = $"R {row.NetVATAmount:N2}"
			});

			TransactionDetails.Add(new TransactionDetail
			{
				Field = "Status",
				Value = row.VATStatus ?? "Not Set"
			});
		}

		private void UpdateSummary()
		{
			decimal totalInputVAT = VATTransactions.Sum(r => r.InputVAT);
			decimal totalOutputVAT = VATTransactions.Sum(r => r.OutputVAT);
			decimal netVATPosition = totalOutputVAT - totalInputVAT;

			TotalInputVATText.Text = $"R {totalInputVAT:N2}";
			TotalOutputVATText.Text = $"R {totalOutputVAT:N2}";
			NetVATPositionText.Text = $"R {netVATPosition:N2}";

			// Color code the net position
			if (netVATPosition > 0)
			{
				NetVATPositionText.Foreground = System.Windows.Media.Brushes.Red;
				NetVATPositionText.Text += " (Payable to SARS)";
			}
			else if (netVATPosition < 0)
			{
				NetVATPositionText.Foreground = System.Windows.Media.Brushes.Green;
				NetVATPositionText.Text += " (Receivable from SARS)";
			}
			else
			{
				NetVATPositionText.Foreground = System.Windows.Media.Brushes.Gray;
				NetVATPositionText.Text += " (Neutral)";
			}
		}
	}

	public class VATRow : INotifyPropertyChanged
	{
		private string? _description;
		private decimal _amountInclVAT;
		private decimal _amountExclVAT;
		private decimal _vatRate = 15;
		private decimal _vatAmount;
		private decimal _inputVAT;
		private decimal _outputVAT;
		private decimal _netVATAmount;
		private string? _vatStatus;
		private string? _transactionType;
		private bool _isVATApplicable = true;
		private bool _isCalculating = false;

		public string? Description
		{
			get => _description;
			set { _description = value; OnPropertyChanged(); }
		}

		public decimal AmountInclVAT
		{
			get => _amountInclVAT;
			set
			{
				if (_amountInclVAT != value && !_isCalculating)
				{
					_amountInclVAT = value;
					OnPropertyChanged();
					CalculateFromInclusive();
				}
			}
		}

		public decimal AmountExclVAT
		{
			get => _amountExclVAT;
			set
			{
				if (_amountExclVAT != value && !_isCalculating)
				{
					_amountExclVAT = value;
					OnPropertyChanged();
					CalculateFromExclusive();
				}
			}
		}

		public decimal VATRate
		{
			get => _vatRate;
			set { _vatRate = value; OnPropertyChanged(); }
		}

		public decimal VATAmount
		{
			get => _vatAmount;
			private set
			{
				_vatAmount = value;
				OnPropertyChanged();
			}
		}

		public string? TransactionType
		{
			get => _transactionType;
			set { _transactionType = value; OnPropertyChanged(); }
		}

		public bool IsVATApplicable
		{
			get => _isVATApplicable;
			set { _isVATApplicable = value; OnPropertyChanged(); }
		}

		public decimal InputVAT
		{
			get => _inputVAT;
			private set
			{
				_inputVAT = value;
				OnPropertyChanged();
			}
		}

		public decimal OutputVAT
		{
			get => _outputVAT;
			private set
			{
				_outputVAT = value;
				OnPropertyChanged();
			}
		}

		public decimal NetVATAmount
		{
			get => _netVATAmount;
			private set
			{
				_netVATAmount = value;
				OnPropertyChanged();
			}
		}

		public string? VATStatus
		{
			get => _vatStatus;
			private set
			{
				_vatStatus = value;
				OnPropertyChanged();
			}
		}

		public void CalculateFromInclusive()
		{
			if (_isCalculating) return;

			_isCalculating = true;
			try
			{
				if (!IsVATApplicable)
				{
					_amountExclVAT = AmountInclVAT;
					VATAmount = 0;
				}
				else if (AmountInclVAT > 0 && VATRate > 0)
				{
					_amountExclVAT = AmountInclVAT / (1 + (VATRate / 100));
					VATAmount = AmountInclVAT - _amountExclVAT;
				}
				else if (AmountInclVAT > 0 && VATRate == 0)
				{
					_amountExclVAT = AmountInclVAT;
					VATAmount = 0;
				}
				OnPropertyChanged(nameof(AmountExclVAT));
			}
			finally
			{
				_isCalculating = false;
			}
		}

		public void CalculateFromExclusive()
		{
			if (_isCalculating) return;

			_isCalculating = true;
			try
			{
				if (!IsVATApplicable)
				{
					_amountInclVAT = AmountExclVAT;
					VATAmount = 0;
				}
				else if (AmountExclVAT > 0 && VATRate > 0)
				{
					VATAmount = AmountExclVAT * (VATRate / 100);
					_amountInclVAT = AmountExclVAT + VATAmount;
				}
				else if (AmountExclVAT > 0 && VATRate == 0)
				{
					_amountInclVAT = AmountExclVAT;
					VATAmount = 0;
				}
				OnPropertyChanged(nameof(AmountInclVAT));
			}
			finally
			{
				_isCalculating = false;
			}
		}

		public void RecalculateVAT()
		{
			if (!IsVATApplicable)
			{
				VATAmount = 0;
				InputVAT = 0;
				OutputVAT = 0;
				NetVATAmount = 0;
				VATStatus = "Not Applicable (No VAT)";

				if (AmountExclVAT > 0 && AmountInclVAT == 0)
				{
					_amountInclVAT = AmountExclVAT;
					OnPropertyChanged(nameof(AmountInclVAT));
				}
				else if (AmountInclVAT > 0 && AmountExclVAT == 0)
				{
					_amountExclVAT = AmountInclVAT;
					OnPropertyChanged(nameof(AmountExclVAT));
				}

				return;
			}

			if (VATAmount == 0 && AmountExclVAT > 0)
			{
				VATAmount = AmountExclVAT * (VATRate / 100);
			}
			else if (VATAmount == 0 && AmountInclVAT > 0)
			{
				CalculateFromInclusive();
			}

			if (TransactionType == "Receivable")
			{
				InputVAT = VATAmount;
				OutputVAT = 0;
				NetVATAmount = -VATAmount;
				VATStatus = VATAmount > 0 ? "Receivable from SARS" : "Neutral";
			}
			else if (TransactionType == "Payable")
			{
				InputVAT = 0;
				OutputVAT = VATAmount;
				NetVATAmount = VATAmount;
				VATStatus = VATAmount > 0 ? "Payable to SARS" : "Neutral";
			}
			else
			{
				InputVAT = 0;
				OutputVAT = 0;
				NetVATAmount = 0;
				VATStatus = "Not Set - Select Transaction Type";
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class TransactionDetail : INotifyPropertyChanged
	{
		private string? _field;
		private string? _value;

		public string? Field
		{
			get => _field;
			set { _field = value; OnPropertyChanged(); }
		}

		public string? Value
		{
			get => _value;
			set { _value = value; OnPropertyChanged(); }
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace StockTracker.Views
{
	public partial class MarkUpCalculation : UserControl
	{
		public ObservableCollection<MarkUpItem> MarkUpItems { get; set; }
		private int rowCounter = 1;
		private const decimal VAT_RATE = 0.15m; // 15% VAT

		public MarkUpCalculation()
		{
			InitializeComponent();
			MarkUpItems = new ObservableCollection<MarkUpItem>();
			MarkUpDataGrid.ItemsSource = MarkUpItems;
		}

		private void AddRow_Click(object sender, RoutedEventArgs e)
		{
			var newItem = new MarkUpItem
			{
				RowNumber = rowCounter++
			};
			MarkUpItems.Add(newItem);
		}

		private void RemoveRow_Click(object sender, RoutedEventArgs e)
		{
			if (MarkUpDataGrid.SelectedItem is MarkUpItem selectedItem)
			{
				MarkUpItems.Remove(selectedItem);
			}
			else
			{
				MessageBox.Show("Please select a row to remove.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void Calculate_Click(object sender, RoutedEventArgs e)
		{
			if (MarkUpItems.Count == 0)
			{
				MessageBox.Show("Please add at least one row before calculating.", "No Rows", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			bool hasErrors = false;
			foreach (var item in MarkUpItems)
			{
				if (!CalculateMarkUp(item))
				{
					hasErrors = true;
				}
			}

			if (!hasErrors)
			{
				MessageBox.Show("Calculations completed successfully! Calculated fields are highlighted in green.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			// Refresh the grid to update highlighting
			MarkUpDataGrid.Items.Refresh();
		}

		private void MarkUpDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			// This ensures the row is refreshed when loaded
			e.Row.UpdateLayout();
		}

		private bool CalculateMarkUp(MarkUpItem item)
		{
			// Reset all calculation flags before calculating
			item.ResetCalculationFlags();

			// Count how many fields are filled
			int filledCount = 0;
			if (item.CostPrice.HasValue && item.CostPrice > 0) filledCount++;
			if (item.MarkUpPercent.HasValue && item.MarkUpPercent >= 0) filledCount++;
			if (!string.IsNullOrEmpty(item.MarkUpBasis)) filledCount++;
			if (item.SellingPrice.HasValue && item.SellingPrice > 0) filledCount++;
			if (item.VATAmount.HasValue && item.VATAmount > 0) filledCount++;
			if (item.MarkedPrice.HasValue && item.MarkedPrice > 0) filledCount++;

			if (filledCount == 0)
			{
				// No data to calculate
				return true;
			}

			if (filledCount < 2)
			{
				MessageBox.Show($"Row {item.RowNumber}: Please provide at least 2 values to calculate.",
					"Insufficient Data", MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			try
			{
				// Pre-process: Derive values from VAT Amount or Marked Price if provided

				// If VAT Amount is provided without Selling Price, derive Selling Price
				if (item.VATAmount.HasValue && item.VATAmount > 0 && !item.SellingPrice.HasValue)
				{
					item.SellingPrice = item.VATAmount.Value / VAT_RATE;
					item.IsSellingPriceCalculated = true;
				}

				// If Marked Price is provided without Selling Price and VAT, derive both
				if (item.MarkedPrice.HasValue && item.MarkedPrice > 0 && !item.SellingPrice.HasValue)
				{
					item.SellingPrice = item.MarkedPrice.Value / (1 + VAT_RATE);
					item.IsSellingPriceCalculated = true;
				}

				// If both Selling Price and VAT Amount are provided, derive Marked Price
				if (item.SellingPrice.HasValue && item.VATAmount.HasValue && !item.MarkedPrice.HasValue)
				{
					item.MarkedPrice = item.SellingPrice.Value + item.VATAmount.Value;
					item.IsMarkedPriceCalculated = true;
				}

				// If Selling Price and Marked Price are provided, derive VAT Amount
				if (item.SellingPrice.HasValue && item.MarkedPrice.HasValue && !item.VATAmount.HasValue)
				{
					item.VATAmount = item.MarkedPrice.Value - item.SellingPrice.Value;
					item.IsVATAmountCalculated = true;
				}

				// Determine if we need to calculate the basis
				bool needToCalculateBasis = string.IsNullOrEmpty(item.MarkUpBasis);

				// If we have Cost, Selling Price, and Mark Up %, we can determine the basis
				if (needToCalculateBasis && item.CostPrice.HasValue && item.SellingPrice.HasValue &&
					item.MarkUpPercent.HasValue && item.CostPrice > 0 && item.SellingPrice > 0 && item.MarkUpPercent >= 0)
				{
					// Calculate markup amount
					decimal markupAmount = item.SellingPrice.Value - item.CostPrice.Value;

					// Calculate what the markup % would be on cost
					decimal markupOnCost = (markupAmount / item.CostPrice.Value) * 100;

					// Calculate what the markup % would be on selling price
					decimal markupOnSelling = (markupAmount / item.SellingPrice.Value) * 100;

					// Determine which one matches the provided markup %
					// Allow for small rounding differences (within 0.1%)
					if (Math.Abs(markupOnCost - item.MarkUpPercent.Value) < 0.1m)
					{
						item.MarkUpBasis = "On Cost Price";
						item.IsMarkUpBasisCalculated = true;
					}
					else if (Math.Abs(markupOnSelling - item.MarkUpPercent.Value) < 0.1m)
					{
						item.MarkUpBasis = "On Selling Price";
						item.IsMarkUpBasisCalculated = true;
					}
					else
					{
						MessageBox.Show($"Row {item.RowNumber}: The provided Mark Up % ({item.MarkUpPercent.Value:N2}%) doesn't match either basis calculation.\n" +
							$"On Cost: {markupOnCost:N2}%\nOn Selling Price: {markupOnSelling:N2}%\n" +
							$"Please verify your inputs or select a basis manually.",
							"Inconsistent Data", MessageBoxButton.OK, MessageBoxImage.Warning);
						return false;
					}
				}
				else if (needToCalculateBasis)
				{
					// If we still need a basis but can't calculate it, try defaulting
					if (item.CostPrice.HasValue || item.SellingPrice.HasValue || item.MarkUpPercent.HasValue)
					{
						MessageBox.Show($"Row {item.RowNumber}: Cannot determine Mark Up Basis. Please select it manually or provide Cost Price, Selling Price, and Mark Up % together.",
							"Missing Basis", MessageBoxButton.OK, MessageBoxImage.Warning);
						return false;
					}
				}

				bool isOnCostBasis = item.MarkUpBasis == "On Cost Price";

				// Main Calculation Scenarios

				// Scenario 1: Cost Price + Mark Up %
				if (item.CostPrice.HasValue && item.MarkUpPercent.HasValue && item.CostPrice > 0 && item.MarkUpPercent >= 0 && !item.SellingPrice.HasValue)
				{
					if (isOnCostBasis)
					{
						item.SellingPrice = item.CostPrice.Value * (1 + (item.MarkUpPercent.Value / 100));
					}
					else
					{
						if (item.MarkUpPercent.Value >= 100)
						{
							MessageBox.Show($"Row {item.RowNumber}: Mark Up % on Selling Price must be less than 100%.",
								"Invalid Value", MessageBoxButton.OK, MessageBoxImage.Warning);
							return false;
						}
						item.SellingPrice = item.CostPrice.Value / (1 - (item.MarkUpPercent.Value / 100));
					}
					item.IsSellingPriceCalculated = true;
				}
				// Scenario 2: Cost Price + Selling Price
				else if (item.CostPrice.HasValue && item.SellingPrice.HasValue && item.CostPrice > 0 && item.SellingPrice > 0 && !item.MarkUpPercent.HasValue)
				{
					if (item.SellingPrice.Value <= item.CostPrice.Value)
					{
						MessageBox.Show($"Row {item.RowNumber}: Selling Price must be greater than Cost Price.",
							"Invalid Value", MessageBoxButton.OK, MessageBoxImage.Warning);
						return false;
					}

					if (isOnCostBasis)
					{
						item.MarkUpPercent = ((item.SellingPrice.Value - item.CostPrice.Value) / item.CostPrice.Value) * 100;
					}
					else
					{
						item.MarkUpPercent = ((item.SellingPrice.Value - item.CostPrice.Value) / item.SellingPrice.Value) * 100;
					}
					item.IsMarkUpPercentCalculated = true;
				}
				// Scenario 3: Selling Price + Mark Up %
				else if (item.SellingPrice.HasValue && item.MarkUpPercent.HasValue && item.SellingPrice > 0 && item.MarkUpPercent >= 0 && !item.CostPrice.HasValue)
				{
					if (isOnCostBasis)
					{
						item.CostPrice = item.SellingPrice.Value / (1 + (item.MarkUpPercent.Value / 100));
					}
					else
					{
						if (item.MarkUpPercent.Value >= 100)
						{
							MessageBox.Show($"Row {item.RowNumber}: Mark Up % on Selling Price must be less than 100%.",
								"Invalid Value", MessageBoxButton.OK, MessageBoxImage.Warning);
							return false;
						}
						item.CostPrice = item.SellingPrice.Value * (1 - (item.MarkUpPercent.Value / 100));
					}
					item.IsCostPriceCalculated = true;
				}

				// Post-process: Calculate VAT and Marked Price if not already provided
				if (item.SellingPrice.HasValue && item.SellingPrice > 0)
				{
					// Calculate VAT Amount if not provided
					if (!item.VATAmount.HasValue || item.VATAmount == 0)
					{
						item.VATAmount = item.SellingPrice.Value * VAT_RATE;
						item.IsVATAmountCalculated = true;
					}

					// Calculate Marked Price if not provided
					if (!item.MarkedPrice.HasValue || item.MarkedPrice == 0)
					{
						item.MarkedPrice = item.SellingPrice.Value + item.VATAmount.Value;
						item.IsMarkedPriceCalculated = true;
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Row {item.RowNumber}: Calculation error - {ex.Message}",
					"Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
		}
	}

	public class MarkUpItem : INotifyPropertyChanged
	{
		private int _rowNumber;
		private decimal? _costPrice;
		private decimal? _markUpPercent;
		private string _markUpBasis;
		private decimal? _sellingPrice;
		private decimal? _vatAmount;
		private decimal? _markedPrice;

		// Calculation tracking flags
		private bool _isCostPriceCalculated;
		private bool _isMarkUpPercentCalculated;
		private bool _isMarkUpBasisCalculated;
		private bool _isSellingPriceCalculated;
		private bool _isVATAmountCalculated;
		private bool _isMarkedPriceCalculated;

		public int RowNumber
		{
			get => _rowNumber;
			set
			{
				if (_rowNumber != value)
				{
					_rowNumber = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? CostPrice
		{
			get => _costPrice;
			set
			{
				if (_costPrice != value)
				{
					_costPrice = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? MarkUpPercent
		{
			get => _markUpPercent;
			set
			{
				if (_markUpPercent != value)
				{
					_markUpPercent = value;
					OnPropertyChanged();
				}
			}
		}

		public string MarkUpBasis
		{
			get => _markUpBasis;
			set
			{
				if (_markUpBasis != value)
				{
					_markUpBasis = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? SellingPrice
		{
			get => _sellingPrice;
			set
			{
				if (_sellingPrice != value)
				{
					_sellingPrice = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? VATAmount
		{
			get => _vatAmount;
			set
			{
				if (_vatAmount != value)
				{
					_vatAmount = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? MarkedPrice
		{
			get => _markedPrice;
			set
			{
				if (_markedPrice != value)
				{
					_markedPrice = value;
					OnPropertyChanged();
				}
			}
		}

		// Calculation tracking properties
		public bool IsCostPriceCalculated
		{
			get => _isCostPriceCalculated;
			set
			{
				if (_isCostPriceCalculated != value)
				{
					_isCostPriceCalculated = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsMarkUpPercentCalculated
		{
			get => _isMarkUpPercentCalculated;
			set
			{
				if (_isMarkUpPercentCalculated != value)
				{
					_isMarkUpPercentCalculated = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsMarkUpBasisCalculated
		{
			get => _isMarkUpBasisCalculated;
			set
			{
				if (_isMarkUpBasisCalculated != value)
				{
					_isMarkUpBasisCalculated = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsSellingPriceCalculated
		{
			get => _isSellingPriceCalculated;
			set
			{
				if (_isSellingPriceCalculated != value)
				{
					_isSellingPriceCalculated = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsVATAmountCalculated
		{
			get => _isVATAmountCalculated;
			set
			{
				if (_isVATAmountCalculated != value)
				{
					_isVATAmountCalculated = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsMarkedPriceCalculated
		{
			get => _isMarkedPriceCalculated;
			set
			{
				if (_isMarkedPriceCalculated != value)
				{
					_isMarkedPriceCalculated = value;
					OnPropertyChanged();
				}
			}
		}

		public void ResetCalculationFlags()
		{
			IsCostPriceCalculated = false;
			IsMarkUpPercentCalculated = false;
			IsMarkUpBasisCalculated = false;
			IsSellingPriceCalculated = false;
			IsVATAmountCalculated = false;
			IsMarkedPriceCalculated = false;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
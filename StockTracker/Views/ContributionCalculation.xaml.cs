using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace StockTracker.Views
{
	public partial class ContributionCalculation : UserControl
	{
		public ObservableCollection<ContributionRow> Contributions { get; set; }
		public ObservableCollection<CalculationDetail> CalculationDetails { get; set; }

		public ContributionCalculation()
		{
			InitializeComponent();
			Contributions = new ObservableCollection<ContributionRow>();
			CalculationDetails = new ObservableCollection<CalculationDetail>();

			ContributionDataGrid.ItemsSource = Contributions;
			CalculationDetailsGrid.ItemsSource = CalculationDetails;
		}

		private void AddRow_Click(object sender, RoutedEventArgs e)
		{
			var newRow = new ContributionRow();
			Contributions.Add(newRow);

			// Select the newly added row
			ContributionDataGrid.SelectedItem = newRow;
			ContributionDataGrid.ScrollIntoView(newRow);
		}

		private void RemoveRow_Click(object sender, RoutedEventArgs e)
		{
			if (ContributionDataGrid.SelectedItem is ContributionRow selectedRow)
			{
				Contributions.Remove(selectedRow);

				if (Contributions.Count == 0)
				{
					CalculationDetailsPanel.Visibility = Visibility.Collapsed;
					CalculationDetails.Clear();
				}
			}
		}

		private void ContributionDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
		{
			if (e.EditAction == DataGridEditAction.Commit)
			{
				// Delay calculation to allow the binding to update
				Dispatcher.BeginInvoke(new Action(() =>
				{
					if (e.Row.Item is ContributionRow row)
					{
						row.RecalculateFields();
						UpdateCalculationDetails(row);
					}
				}), System.Windows.Threading.DispatcherPriority.Background);
			}
		}

		private void ContributionDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ContributionDataGrid.SelectedItem is ContributionRow selectedRow)
			{
				UpdateCalculationDetails(selectedRow);
			}
		}

		private void UpdateCalculationDetails(ContributionRow row)
		{
			CalculationDetails.Clear();

			if (row == null)
			{
				CalculationDetailsPanel.Visibility = Visibility.Collapsed;
				return;
			}

			CalculationDetailsPanel.Visibility = Visibility.Visible;

			// Contribution per unit
			if (row.ContributionPerUnit.HasValue)
			{
				if (row.SellingPrice.HasValue && row.VariableCosts.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Contribution per unit",
						Formula = "Selling Price - Variable Costs",
						SubstitutedValues = $"{row.SellingPrice:N2} - {row.VariableCosts:N2}",
						Result = row.ContributionPerUnit.Value.ToString("N2")
					});
				}
				else if (row.SellingPrice.HasValue && row.ContributionMarginRatio.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Contribution per unit",
						Formula = "Selling Price × Contribution Margin Ratio",
						SubstitutedValues = $"{row.SellingPrice:N2} × {row.ContributionMarginRatio:N3}",
						Result = row.ContributionPerUnit.Value.ToString("N2")
					});
				}
			}

			// Contribution Margin Ratio
			if (row.ContributionMarginRatio.HasValue)
			{
				if (row.ContributionPerUnit.HasValue && row.SellingPrice.HasValue && row.SellingPrice.Value != 0)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Contribution Margin Ratio",
						Formula = "Contribution per unit / Selling Price",
						SubstitutedValues = $"{row.ContributionPerUnit:N2} / {row.SellingPrice:N2}",
						Result = row.ContributionMarginRatio.Value.ToString("N3")
					});
				}
				else if (row.SellingPrice.HasValue && row.VariableCosts.HasValue && row.SellingPrice.Value != 0)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Contribution Margin Ratio",
						Formula = "(Selling Price - Variable Costs) / Selling Price",
						SubstitutedValues = $"({row.SellingPrice:N2} - {row.VariableCosts:N2}) / {row.SellingPrice:N2}",
						Result = row.ContributionMarginRatio.Value.ToString("N3")
					});
				}
			}

			// Selling Price
			if (row.SellingPrice.HasValue)
			{
				if (row.VariableCosts.HasValue && row.ContributionPerUnit.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Selling Price",
						Formula = "Variable Costs + Contribution per unit",
						SubstitutedValues = $"{row.VariableCosts:N2} + {row.ContributionPerUnit:N2}",
						Result = row.SellingPrice.Value.ToString("N2")
					});
				}
				else if (row.ContributionPerUnit.HasValue && row.ContributionMarginRatio.HasValue && row.ContributionMarginRatio.Value > 0)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Selling Price",
						Formula = "Contribution per unit / Contribution Margin Ratio",
						SubstitutedValues = $"{row.ContributionPerUnit:N2} / {row.ContributionMarginRatio:N3}",
						Result = row.SellingPrice.Value.ToString("N2")
					});
				}
			}

			// Variable Costs
			if (row.VariableCosts.HasValue)
			{
				if (row.SellingPrice.HasValue && row.ContributionPerUnit.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Variable Costs",
						Formula = "Selling Price - Contribution per unit",
						SubstitutedValues = $"{row.SellingPrice:N2} - {row.ContributionPerUnit:N2}",
						Result = row.VariableCosts.Value.ToString("N2")
					});
				}
				else if (row.SellingPrice.HasValue && row.ContributionMarginRatio.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Variable Costs",
						Formula = "Selling Price × (1 - Contribution Margin Ratio)",
						SubstitutedValues = $"{row.SellingPrice:N2} × (1 - {row.ContributionMarginRatio:N3})",
						Result = row.VariableCosts.Value.ToString("N2")
					});
				}
			}

			// Breakeven Quantity
			if (row.BreakevenQuantity.HasValue)
			{
				if (row.FixedCosts.HasValue && row.ContributionPerUnit.HasValue && row.ContributionPerUnit.Value != 0)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Breakeven Quantity",
						Formula = "Fixed Costs / Contribution per unit",
						SubstitutedValues = $"{row.FixedCosts:N2} / {row.ContributionPerUnit:N2}",
						Result = row.BreakevenQuantity.Value.ToString("N0")
					});
				}
				else if (row.BreakevenValue.HasValue && row.SellingPrice.HasValue && row.SellingPrice.Value != 0)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Breakeven Quantity",
						Formula = "Breakeven Value / Selling Price",
						SubstitutedValues = $"{row.BreakevenValue:N2} / {row.SellingPrice:N2}",
						Result = row.BreakevenQuantity.Value.ToString("N0")
					});
				}
			}

			// Fixed Costs
			if (row.FixedCosts.HasValue)
			{
				if (row.BreakevenQuantity.HasValue && row.ContributionPerUnit.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Fixed Costs",
						Formula = "Breakeven Quantity × Contribution per unit",
						SubstitutedValues = $"{row.BreakevenQuantity:N0} × {row.ContributionPerUnit:N2}",
						Result = row.FixedCosts.Value.ToString("N2")
					});
				}
			}

			// Breakeven Value
			if (row.BreakevenValue.HasValue)
			{
				if (row.BreakevenQuantity.HasValue && row.SellingPrice.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Breakeven Value",
						Formula = "Breakeven Quantity × Selling Price",
						SubstitutedValues = $"{row.BreakevenQuantity:N0} × {row.SellingPrice:N2}",
						Result = row.BreakevenValue.Value.ToString("N2")
					});
				}
				else if (row.FixedCosts.HasValue && row.ContributionMarginRatio.HasValue && row.ContributionMarginRatio.Value != 0)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Breakeven Value",
						Formula = "Fixed Costs / Contribution Margin Ratio",
						SubstitutedValues = $"{row.FixedCosts:N2} / {row.ContributionMarginRatio:N3}",
						Result = row.BreakevenValue.Value.ToString("N2")
					});
				}
			}
		}
	}

	public class CalculationDetail
	{
		public string Field { get; set; }
		public string Formula { get; set; }
		public string SubstitutedValues { get; set; }
		public string Result { get; set; }
	}

	public class ContributionRow : INotifyPropertyChanged
	{
		private decimal? _fixedCosts;
		private decimal? _variableCosts;
		private decimal? _contributionMarginRatio;
		private decimal? _sellingPrice;
		private decimal? _contributionPerUnit;
		private decimal? _breakevenQuantity;
		private decimal? _breakevenValue;

		public decimal? FixedCosts
		{
			get => _fixedCosts;
			set
			{
				if (_fixedCosts != value)
				{
					_fixedCosts = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? VariableCosts
		{
			get => _variableCosts;
			set
			{
				if (_variableCosts != value)
				{
					_variableCosts = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? ContributionMarginRatio
		{
			get => _contributionMarginRatio;
			set
			{
				if (_contributionMarginRatio != value)
				{
					_contributionMarginRatio = value;
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

		public decimal? ContributionPerUnit
		{
			get => _contributionPerUnit;
			set
			{
				if (_contributionPerUnit != value)
				{
					_contributionPerUnit = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? BreakevenQuantity
		{
			get => _breakevenQuantity;
			set
			{
				if (_breakevenQuantity != value)
				{
					_breakevenQuantity = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? BreakevenValue
		{
			get => _breakevenValue;
			set
			{
				if (_breakevenValue != value)
				{
					_breakevenValue = value;
					OnPropertyChanged();
				}
			}
		}

		public void RecalculateFields()
		{
			// Calculate Contribution per unit if possible
			if (SellingPrice.HasValue && VariableCosts.HasValue && !ContributionPerUnit.HasValue)
			{
				ContributionPerUnit = SellingPrice.Value - VariableCosts.Value;
			}
			else if (SellingPrice.HasValue && ContributionMarginRatio.HasValue && !ContributionPerUnit.HasValue)
			{
				ContributionPerUnit = SellingPrice.Value * ContributionMarginRatio.Value;
			}

			// Calculate Selling Price if missing
			if (!SellingPrice.HasValue && VariableCosts.HasValue && ContributionPerUnit.HasValue)
			{
				SellingPrice = VariableCosts.Value + ContributionPerUnit.Value;
			}
			else if (!SellingPrice.HasValue && ContributionPerUnit.HasValue && ContributionMarginRatio.HasValue && ContributionMarginRatio.Value > 0)
			{
				SellingPrice = ContributionPerUnit.Value / ContributionMarginRatio.Value;
			}

			// Calculate Variable Costs if missing
			if (!VariableCosts.HasValue && SellingPrice.HasValue && ContributionPerUnit.HasValue)
			{
				VariableCosts = SellingPrice.Value - ContributionPerUnit.Value;
			}
			else if (!VariableCosts.HasValue && SellingPrice.HasValue && ContributionMarginRatio.HasValue)
			{
				VariableCosts = SellingPrice.Value * (1 - ContributionMarginRatio.Value);
			}

			// Calculate Contribution Margin Ratio if missing
			if (!ContributionMarginRatio.HasValue && SellingPrice.HasValue && SellingPrice.Value != 0 && ContributionPerUnit.HasValue)
			{
				ContributionMarginRatio = ContributionPerUnit.Value / SellingPrice.Value;
			}
			else if (!ContributionMarginRatio.HasValue && SellingPrice.HasValue && VariableCosts.HasValue && SellingPrice.Value != 0)
			{
				ContributionMarginRatio = (SellingPrice.Value - VariableCosts.Value) / SellingPrice.Value;
			}

			// Calculate Breakeven Quantity if possible
			if (!BreakevenQuantity.HasValue && FixedCosts.HasValue && ContributionPerUnit.HasValue && ContributionPerUnit.Value != 0)
			{
				BreakevenQuantity = FixedCosts.Value / ContributionPerUnit.Value;
			}

			// Calculate Fixed Costs if missing
			if (!FixedCosts.HasValue && BreakevenQuantity.HasValue && ContributionPerUnit.HasValue)
			{
				FixedCosts = BreakevenQuantity.Value * ContributionPerUnit.Value;
			}

			// Calculate Breakeven Value if possible
			if (!BreakevenValue.HasValue && BreakevenQuantity.HasValue && SellingPrice.HasValue)
			{
				BreakevenValue = BreakevenQuantity.Value * SellingPrice.Value;
			}
			else if (!BreakevenValue.HasValue && FixedCosts.HasValue && ContributionMarginRatio.HasValue && ContributionMarginRatio.Value != 0)
			{
				BreakevenValue = FixedCosts.Value / ContributionMarginRatio.Value;
			}

			// Recalculate BreakevenQuantity from BreakevenValue if needed
			if (BreakevenValue.HasValue && SellingPrice.HasValue && SellingPrice.Value != 0 && !BreakevenQuantity.HasValue)
			{
				BreakevenQuantity = BreakevenValue.Value / SellingPrice.Value;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace StockTracker.Views
{
	public partial class FixedCostPortionCalculator : UserControl
	{
		public ObservableCollection<CostDataEntry> CostEntries { get; set; }
		private int entryCounter = 1;

		public FixedCostPortionCalculator()
		{
			InitializeComponent();
			CostEntries = new ObservableCollection<CostDataEntry>();
			CostDataGrid.ItemsSource = CostEntries;
		}

		private void AddEntry_Click(object sender, RoutedEventArgs e)
		{
			var newEntry = new CostDataEntry
			{
				EntryNumber = entryCounter++
			};
			CostEntries.Add(newEntry);
		}

		private void RemoveEntry_Click(object sender, RoutedEventArgs e)
		{
			if (CostDataGrid.SelectedItem is CostDataEntry selectedEntry)
			{
				CostEntries.Remove(selectedEntry);
				// Renumber entries
				int counter = 1;
				foreach (var entry in CostEntries)
				{
					entry.EntryNumber = counter++;
				}
				entryCounter = counter;
			}
			else
			{
				MessageBox.Show("Please select an entry to remove.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void Calculate_Click(object sender, RoutedEventArgs e)
		{
			// Validate minimum entries
			if (CostEntries.Count < 2)
			{
				MessageBox.Show("Please add at least 2 entries to calculate the fixed cost portion.",
					"Insufficient Data", MessageBoxButton.OK, MessageBoxImage.Warning);
				HideResults();
				return;
			}

			// Validate all entries have values
			var invalidEntries = CostEntries.Where(entry =>
				entry.Units <= 0 || entry.TotalCostPrice <= 0).ToList();

			if (invalidEntries.Any())
			{
				MessageBox.Show("All entries must have valid Units (greater than 0) and Total Cost Price (greater than 0).",
					"Invalid Data", MessageBoxButton.OK, MessageBoxImage.Warning);
				HideResults();
				return;
			}

			// Check for duplicate unit quantities
			var duplicateUnits = CostEntries.GroupBy(e => e.Units)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicateUnits.Any())
			{
				MessageBox.Show($"Duplicate unit quantities found: {string.Join(", ", duplicateUnits)}. Each entry should have a different number of units for accurate calculation.",
					"Duplicate Units", MessageBoxButton.OK, MessageBoxImage.Warning);
				HideResults();
				return;
			}

			// Find high and low activity levels
			var highActivityEntry = CostEntries.OrderByDescending(e => e.Units).First();
			var lowActivityEntry = CostEntries.OrderBy(e => e.Units).First();

			// Calculate Variable Cost Per Unit using High-Low Method
			decimal variableCostPerUnit = (highActivityEntry.TotalCostPrice - lowActivityEntry.TotalCostPrice) /
										   (highActivityEntry.Units - lowActivityEntry.Units);

			// Calculate Fixed Cost Portion
			decimal fixedCostPortion = highActivityEntry.TotalCostPrice - (variableCostPerUnit * highActivityEntry.Units);

			// Alternative calculation using low activity (should give same result)
			// decimal fixedCostPortionAlt = lowActivityEntry.TotalCostPrice - (variableCostPerUnit * lowActivityEntry.Units);

			// Display results
			DisplayResults(variableCostPerUnit, fixedCostPortion, highActivityEntry, lowActivityEntry);
		}

		private void DisplayResults(decimal variableCostPerUnit, decimal fixedCostPortion,
			CostDataEntry highActivity, CostDataEntry lowActivity)
		{
			// Show panels
			FixedCostPortionPanel.Visibility = Visibility.Visible;
			VariableCostPanel.Visibility = Visibility.Visible;
			CostEquationPanel.Visibility = Visibility.Visible;

			// Variable Cost Per Unit
			VariableCostFormulaText.Text =
				$"Variable Cost per Unit = (R {highActivity.TotalCostPrice:N2} - R {lowActivity.TotalCostPrice:N2}) ÷ ({highActivity.Units:N0} - {lowActivity.Units:N0})\n" +
				$"Variable Cost per Unit = R {highActivity.TotalCostPrice - lowActivity.TotalCostPrice:N2} ÷ {highActivity.Units - lowActivity.Units:N0}\n" +
				$"Variable Cost per Unit = R {variableCostPerUnit:N2}";

			VariableCostPerUnitResult.Text = $"R {variableCostPerUnit:N2}";

			// Fixed Cost Portion
			FixedCostFormulaText.Text =
				$"Fixed Cost = R {highActivity.TotalCostPrice:N2} - (R {variableCostPerUnit:N2} × {highActivity.Units:N0})\n" +
				$"Fixed Cost = R {highActivity.TotalCostPrice:N2} - R {variableCostPerUnit * highActivity.Units:N2}\n" +
				$"Fixed Cost = R {fixedCostPortion:N2}";

			FixedCostPortionResult.Text = $"R {fixedCostPortion:N2}";

			// Cost Equation
			CostEquationText.Text = $"Total Cost = R {fixedCostPortion:N2} + (R {variableCostPerUnit:N2} × Q)";

			MessageBox.Show("Calculations completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void HideResults()
		{
			FixedCostPortionPanel.Visibility = Visibility.Collapsed;
			VariableCostPanel.Visibility = Visibility.Collapsed;
			CostEquationPanel.Visibility = Visibility.Collapsed;
		}
	}

	public class CostDataEntry : INotifyPropertyChanged
	{
		private int entryNumber;
		private decimal units;
		private decimal totalCostPrice;

		public int EntryNumber
		{
			get => entryNumber;
			set
			{
				entryNumber = value;
				OnPropertyChanged();
			}
		}

		public decimal Units
		{
			get => units;
			set
			{
				units = value;
				OnPropertyChanged();
			}
		}

		public decimal TotalCostPrice
		{
			get => totalCostPrice;
			set
			{
				totalCostPrice = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
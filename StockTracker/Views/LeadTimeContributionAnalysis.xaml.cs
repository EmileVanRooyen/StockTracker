using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace StockTracker.Views
{
	public partial class LeadTimeContributionAnalysis : UserControl
	{
		public ObservableCollection<LeadTimeRow> LeadTimeRows { get; set; }
		public ObservableCollection<CalculationDetail> CalculationDetails { get; set; }

		public LeadTimeContributionAnalysis()
		{
			InitializeComponent();
			LeadTimeRows = new ObservableCollection<LeadTimeRow>();
			CalculationDetails = new ObservableCollection<CalculationDetail>();

			LeadTimeDataGrid.ItemsSource = LeadTimeRows;
			CalculationDetailsGrid.ItemsSource = CalculationDetails;
		}

		private void AddRow_Click(object sender, RoutedEventArgs e)
		{
			var newRow = new LeadTimeRow();
			LeadTimeRows.Add(newRow);

			// Delay selection to ensure the row is rendered
			Dispatcher.BeginInvoke(new Action(() =>
			{
				LeadTimeDataGrid.SelectedItem = newRow;
				LeadTimeDataGrid.ScrollIntoView(newRow);
				LeadTimeDataGrid.UpdateLayout();

				// Focus the first cell of the new row
				var row = (DataGridRow)LeadTimeDataGrid.ItemContainerGenerator.ContainerFromItem(newRow);
				if (row != null)
				{
					row.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
				}
			}), System.Windows.Threading.DispatcherPriority.Background);
		}

		private void RemoveRow_Click(object sender, RoutedEventArgs e)
		{
			if (LeadTimeDataGrid.SelectedItem is LeadTimeRow selectedRow)
			{
				LeadTimeRows.Remove(selectedRow);

				if (LeadTimeRows.Count == 0)
				{
					CalculationDetailsPanel.Visibility = Visibility.Collapsed;
					CalculationDetails.Clear();
				}
			}
		}

		private void Calculate_Click(object sender, RoutedEventArgs e)
		{
			// Calculate missing fields for all rows
			foreach (var row in LeadTimeRows)
			{
				row.RecalculateFields();
			}

			// Update calculation details for the selected row
			if (LeadTimeDataGrid.SelectedItem is LeadTimeRow selectedRow)
			{
				UpdateCalculationDetails(selectedRow);
			}
		}

		private void LeadTimeDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (LeadTimeDataGrid.SelectedItem is LeadTimeRow selectedRow)
			{
				UpdateCalculationDetails(selectedRow);
			}
		}

		private void UpdateCalculationDetails(LeadTimeRow row)
		{
			CalculationDetails.Clear();

			if (row == null)
			{
				CalculationDetailsPanel.Visibility = Visibility.Collapsed;
				return;
			}

			CalculationDetailsPanel.Visibility = Visibility.Visible;

			// Average Lead Time
			if (row.AverageLeadTime.HasValue)
			{
				if (row.MaximumLeadTime.HasValue && row.MinimumLeadTime.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Average Lead Time",
						Formula = "(Maximum Lead Time + Minimum Lead Time) / 2",
						SubstitutedValues = $"({row.MaximumLeadTime:N1} + {row.MinimumLeadTime:N1}) / 2",
						Result = row.AverageLeadTime.Value.ToString("N1")
					});
				}
			}

			// Average Usage
			if (row.AverageUsage.HasValue)
			{
				if (row.MaximumUsage.HasValue && row.MinimumUsage.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Average Usage",
						Formula = "(Maximum Usage + Minimum Usage) / 2",
						SubstitutedValues = $"({row.MaximumUsage:N0} + {row.MinimumUsage:N0}) / 2",
						Result = row.AverageUsage.Value.ToString("N0")
					});
				}
			}

			// Safety Stock
			if (row.SafetyStock.HasValue)
			{
				if (row.MaximumUsage.HasValue && row.MaximumLeadTime.HasValue &&
					row.AverageUsage.HasValue && row.AverageLeadTime.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Safety Stock",
						Formula = "(Maximum Usage × Maximum Lead Time) - (Average Usage × Average Lead Time)",
						SubstitutedValues = $"({row.MaximumUsage:N0} × {row.MaximumLeadTime:N1}) - ({row.AverageUsage:N0} × {row.AverageLeadTime:N1})",
						Result = row.SafetyStock.Value.ToString("N0")
					});
				}
			}

			// Reorder Point
			if (row.ReorderPoint.HasValue)
			{
				if (row.AverageUsage.HasValue && row.AverageLeadTime.HasValue && row.SafetyStock.HasValue)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Reorder Point",
						Formula = "(Average Usage × Average Lead Time) + Safety Stock",
						SubstitutedValues = $"({row.AverageUsage:N0} × {row.AverageLeadTime:N1}) + {row.SafetyStock:N0}",
						Result = row.ReorderPoint.Value.ToString("N0")
					});
				}
			}

			// Maximum Lead Time (if calculated from other fields)
			if (row.MaximumLeadTime.HasValue && row.AverageLeadTime.HasValue && row.MinimumLeadTime.HasValue)
			{
				// Check if it was calculated (not directly entered)
				var calculatedMax = (row.AverageLeadTime.Value * 2) - row.MinimumLeadTime.Value;
				if (Math.Abs(row.MaximumLeadTime.Value - calculatedMax) < 0.01m)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Maximum Lead Time",
						Formula = "(2 × Average Lead Time) - Minimum Lead Time",
						SubstitutedValues = $"(2 × {row.AverageLeadTime:N1}) - {row.MinimumLeadTime:N1}",
						Result = row.MaximumLeadTime.Value.ToString("N1")
					});
				}
			}

			// Minimum Lead Time (if calculated from other fields)
			if (row.MinimumLeadTime.HasValue && row.AverageLeadTime.HasValue && row.MaximumLeadTime.HasValue)
			{
				// Check if it was calculated (not directly entered)
				var calculatedMin = (row.AverageLeadTime.Value * 2) - row.MaximumLeadTime.Value;
				if (Math.Abs(row.MinimumLeadTime.Value - calculatedMin) < 0.01m)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Minimum Lead Time",
						Formula = "(2 × Average Lead Time) - Maximum Lead Time",
						SubstitutedValues = $"(2 × {row.AverageLeadTime:N1}) - {row.MaximumLeadTime:N1}",
						Result = row.MinimumLeadTime.Value.ToString("N1")
					});
				}
			}

			// Maximum Usage (if calculated from other fields)
			if (row.MaximumUsage.HasValue && row.AverageUsage.HasValue && row.MinimumUsage.HasValue)
			{
				// Check if it was calculated (not directly entered)
				var calculatedMax = (row.AverageUsage.Value * 2) - row.MinimumUsage.Value;
				if (Math.Abs(row.MaximumUsage.Value - calculatedMax) < 0.01m)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Maximum Usage",
						Formula = "(2 × Average Usage) - Minimum Usage",
						SubstitutedValues = $"(2 × {row.AverageUsage:N0}) - {row.MinimumUsage:N0}",
						Result = row.MaximumUsage.Value.ToString("N0")
					});
				}
			}

			// Minimum Usage (if calculated from other fields)
			if (row.MinimumUsage.HasValue && row.AverageUsage.HasValue && row.MaximumUsage.HasValue)
			{
				// Check if it was calculated (not directly entered)
				var calculatedMin = (row.AverageUsage.Value * 2) - row.MaximumUsage.Value;
				if (Math.Abs(row.MinimumUsage.Value - calculatedMin) < 0.01m)
				{
					CalculationDetails.Add(new CalculationDetail
					{
						Field = "Minimum Usage",
						Formula = "(2 × Average Usage) - Maximum Usage",
						SubstitutedValues = $"(2 × {row.AverageUsage:N0}) - {row.MaximumUsage:N0}",
						Result = row.MinimumUsage.Value.ToString("N0")
					});
				}
			}
		}
	}

	public class LeadTimeRow : INotifyPropertyChanged
	{
		private decimal? _maximumLeadTime;
		private decimal? _maximumUsage;
		private decimal? _minimumLeadTime;
		private decimal? _minimumUsage;
		private decimal? _averageLeadTime;
		private decimal? _averageUsage;
		private decimal? _safetyStock;
		private decimal? _reorderPoint;

		public decimal? MaximumLeadTime
		{
			get => _maximumLeadTime;
			set
			{
				if (_maximumLeadTime != value)
				{
					_maximumLeadTime = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? MaximumUsage
		{
			get => _maximumUsage;
			set
			{
				if (_maximumUsage != value)
				{
					_maximumUsage = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? MinimumLeadTime
		{
			get => _minimumLeadTime;
			set
			{
				if (_minimumLeadTime != value)
				{
					_minimumLeadTime = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? MinimumUsage
		{
			get => _minimumUsage;
			set
			{
				if (_minimumUsage != value)
				{
					_minimumUsage = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? AverageLeadTime
		{
			get => _averageLeadTime;
			set
			{
				if (_averageLeadTime != value)
				{
					_averageLeadTime = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? AverageUsage
		{
			get => _averageUsage;
			set
			{
				if (_averageUsage != value)
				{
					_averageUsage = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? SafetyStock
		{
			get => _safetyStock;
			set
			{
				if (_safetyStock != value)
				{
					_safetyStock = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal? ReorderPoint
		{
			get => _reorderPoint;
			set
			{
				if (_reorderPoint != value)
				{
					_reorderPoint = value;
					OnPropertyChanged();
				}
			}
		}

		public void RecalculateFields()
		{
			// Calculate Average Lead Time if possible
			if (!AverageLeadTime.HasValue && MaximumLeadTime.HasValue && MinimumLeadTime.HasValue)
			{
				AverageLeadTime = (MaximumLeadTime.Value + MinimumLeadTime.Value) / 2;
			}

			// Calculate Maximum Lead Time if missing
			if (!MaximumLeadTime.HasValue && AverageLeadTime.HasValue && MinimumLeadTime.HasValue)
			{
				MaximumLeadTime = (AverageLeadTime.Value * 2) - MinimumLeadTime.Value;
			}

			// Calculate Minimum Lead Time if missing
			if (!MinimumLeadTime.HasValue && AverageLeadTime.HasValue && MaximumLeadTime.HasValue)
			{
				MinimumLeadTime = (AverageLeadTime.Value * 2) - MaximumLeadTime.Value;
			}

			// Calculate Average Usage if possible
			if (!AverageUsage.HasValue && MaximumUsage.HasValue && MinimumUsage.HasValue)
			{
				AverageUsage = (MaximumUsage.Value + MinimumUsage.Value) / 2;
			}

			// Calculate Maximum Usage if missing
			if (!MaximumUsage.HasValue && AverageUsage.HasValue && MinimumUsage.HasValue)
			{
				MaximumUsage = (AverageUsage.Value * 2) - MinimumUsage.Value;
			}

			// Calculate Minimum Usage if missing
			if (!MinimumUsage.HasValue && AverageUsage.HasValue && MaximumUsage.HasValue)
			{
				MinimumUsage = (AverageUsage.Value * 2) - MaximumUsage.Value;
			}

			// Calculate Safety Stock if possible
			if (!SafetyStock.HasValue && MaximumUsage.HasValue && MaximumLeadTime.HasValue &&
				AverageUsage.HasValue && AverageLeadTime.HasValue)
			{
				SafetyStock = (MaximumUsage.Value * MaximumLeadTime.Value) - (AverageUsage.Value * AverageLeadTime.Value);
			}

			// Calculate Reorder Point if possible
			if (!ReorderPoint.HasValue && AverageUsage.HasValue && AverageLeadTime.HasValue && SafetyStock.HasValue)
			{
				ReorderPoint = (AverageUsage.Value * AverageLeadTime.Value) + SafetyStock.Value;
			}

			// Reverse calculate Safety Stock from Reorder Point if needed
			if (!SafetyStock.HasValue && ReorderPoint.HasValue && AverageUsage.HasValue && AverageLeadTime.HasValue)
			{
				SafetyStock = ReorderPoint.Value - (AverageUsage.Value * AverageLeadTime.Value);
			}

			// Advanced calculations: Try to derive missing fields from Safety Stock formula
			// If we have Safety Stock and three of the four variables in its formula, calculate the fourth
			if (SafetyStock.HasValue)
			{
				// Calculate Maximum Usage from Safety Stock
				if (!MaximumUsage.HasValue && MaximumLeadTime.HasValue && AverageUsage.HasValue && AverageLeadTime.HasValue && MaximumLeadTime.Value != 0)
				{
					MaximumUsage = (SafetyStock.Value + (AverageUsage.Value * AverageLeadTime.Value)) / MaximumLeadTime.Value;
				}

				// Calculate Maximum Lead Time from Safety Stock
				if (!MaximumLeadTime.HasValue && MaximumUsage.HasValue && AverageUsage.HasValue && AverageLeadTime.HasValue && MaximumUsage.Value != 0)
				{
					MaximumLeadTime = (SafetyStock.Value + (AverageUsage.Value * AverageLeadTime.Value)) / MaximumUsage.Value;
				}

				// Calculate Average Usage from Safety Stock
				if (!AverageUsage.HasValue && MaximumUsage.HasValue && MaximumLeadTime.HasValue && AverageLeadTime.HasValue && AverageLeadTime.Value != 0)
				{
					AverageUsage = ((MaximumUsage.Value * MaximumLeadTime.Value) - SafetyStock.Value) / AverageLeadTime.Value;
				}

				// Calculate Average Lead Time from Safety Stock
				if (!AverageLeadTime.HasValue && MaximumUsage.HasValue && MaximumLeadTime.HasValue && AverageUsage.HasValue && AverageUsage.Value != 0)
				{
					AverageLeadTime = ((MaximumUsage.Value * MaximumLeadTime.Value) - SafetyStock.Value) / AverageUsage.Value;
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace StockTracker.Views
{
	public partial class EOQCalculator : UserControl
	{
		public ObservableCollection<CostItem> OrderingCosts { get; set; }
		public ObservableCollection<CostItem> HoldingCosts { get; set; }

		public EOQCalculator()
		{
			InitializeComponent();
			OrderingCosts = new ObservableCollection<CostItem>();
			HoldingCosts = new ObservableCollection<CostItem>();

			OrderingCostsGrid.ItemsSource = OrderingCosts;
			HoldingCostsGrid.ItemsSource = HoldingCosts;

			// Subscribe to collection changes
			OrderingCosts.CollectionChanged += (s, e) => UpdateTotalOrderingCost();
			HoldingCosts.CollectionChanged += (s, e) => UpdateTotalHoldingCost();
		}

		#region Phase 1: Annual Demand Calculation

		private void DemandInput_TextChanged(object sender, EventArgs e)
		{
			CalculateAnnualDemand();
		}

		private void CalculateAnnualDemand()
		{
			if (AnnualDemandDisplay == null || DemandQuantityInput == null || DemandPeriodInput == null)
				return;

			if (!decimal.TryParse(DemandQuantityInput.Text, out decimal demand) || demand <= 0)
			{
				AnnualDemandDisplay.Text = "0 units";
				return;
			}

			decimal annualDemand = 0;
			string period = (DemandPeriodInput.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Annually";

			switch (period)
			{
				case "Daily":
					annualDemand = demand * 365;
					break;
				case "Weekly":
					annualDemand = demand * 52;
					break;
				case "Monthly":
					annualDemand = demand * 12;
					break;
				case "Annually":
					annualDemand = demand;
					break;
			}

			AnnualDemandDisplay.Text = $"{annualDemand:N0} units";
		}

		private decimal GetAnnualDemand()
		{
			if (!decimal.TryParse(DemandQuantityInput.Text, out decimal demand) || demand <= 0)
				return 0;

			string period = (DemandPeriodInput.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Annually";

			return period switch
			{
				"Daily" => demand * 365,
				"Weekly" => demand * 52,
				"Monthly" => demand * 12,
				"Annually" => demand,
				_ => 0
			};
		}

		#endregion

		#region Phase 2: Ordering Costs

		private void AddOrderingCost_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new CostInputDialog("Ordering Cost");
			if (dialog.ShowDialog() == true)
			{
				OrderingCosts.Add(new CostItem
				{
					Description = dialog.CostDescription,
					Cost = dialog.CostAmount
				});
			}
		}

		private void RemoveOrderingCost_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Tag is CostItem cost)
			{
				OrderingCosts.Remove(cost);
			}
		}

		private void UpdateTotalOrderingCost()
		{
			if (TotalOrderingCostDisplay == null)
				return;

			decimal total = OrderingCosts.Sum(c => c.Cost);
			TotalOrderingCostDisplay.Text = $"R {total:N2}";
		}

		#endregion

		#region Phase 3: Holding Costs

		private void AddHoldingCost_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new CostInputDialog("Holding Cost");
			if (dialog.ShowDialog() == true)
			{
				HoldingCosts.Add(new CostItem
				{
					Description = dialog.CostDescription,
					Cost = dialog.CostAmount
				});
			}
		}

		private void RemoveHoldingCost_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Tag is CostItem cost)
			{
				HoldingCosts.Remove(cost);
			}
		}

		private void UpdateTotalHoldingCost()
		{
			if (TotalHoldingCostDisplay == null)
				return;

			decimal total = HoldingCosts.Sum(c => c.Cost);
			TotalHoldingCostDisplay.Text = $"R {total:N2}";
		}

		#endregion

		#region Phase 4: Reorder Level

		private void ReorderInput_TextChanged(object sender, EventArgs e)
		{
			UpdatePeriodLabels();
			CalculateReorderLevel();
		}

		private void UpdatePeriodLabels()
		{
			if (MaxUsagePeriodLabel == null || MaxLeadTimePeriodLabel == null || LeadTimePeriodInput == null)
				return;

			string period = (LeadTimePeriodInput.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Days";
			string periodLower = period.ToLower();
			string periodSingular = period == "Days" ? "day" : period == "Weeks" ? "week" : "month";

			MaxUsagePeriodLabel.Text = $"units per {periodSingular}";
			MaxLeadTimePeriodLabel.Text = periodLower;
		}

		private void CalculateReorderLevel()
		{
			if (ReorderLevelDisplay == null || ReorderFormulaDisplay == null || LeadTimeInDaysDisplay == null ||
				MaxUsageInput == null || MaxLeadTimeInput == null || LeadTimePeriodInput == null)
				return;

			if (!decimal.TryParse(MaxUsageInput.Text, out decimal maxUsage) || maxUsage <= 0 ||
				!decimal.TryParse(MaxLeadTimeInput.Text, out decimal maxLeadTime) || maxLeadTime <= 0)
			{
				ReorderLevelDisplay.Text = "0 units";
				LeadTimeInDaysDisplay.Text = "";
				ReorderFormulaDisplay.Text = "Formula: Maximum Usage × Maximum Lead Time";
				return;
			}

			string period = (LeadTimePeriodInput.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Days";

			// Calculate Reorder Level (Maximum Usage per period × Lead Time in same period)
			decimal reorderLevel = maxUsage * maxLeadTime;
			ReorderLevelDisplay.Text = $"{reorderLevel:N0} units";

			// Build formula display
			string periodLower = period.ToLower();
			string periodSingular = period == "Days" ? "day" : period == "Weeks" ? "week" : "month";

			ReorderFormulaDisplay.Text = $"Formula: {maxUsage:N0} units/{periodSingular} × {maxLeadTime:N0} {periodLower} = {reorderLevel:N0} units";

			// Show conversion to days for reference
			decimal leadTimeInDays = ConvertPeriodToDays(maxLeadTime, period);
			if (period != "Days")
			{
				LeadTimeInDaysDisplay.Text = $"({leadTimeInDays:N1} days at maximum usage = {reorderLevel:N0} units)";
			}
			else
			{
				LeadTimeInDaysDisplay.Text = "";
			}
		}

		private decimal ConvertPeriodToDays(decimal value, string period)
		{
			return period switch
			{
				"Days" => value,
				"Weeks" => value * 7,
				"Months" => value * 30, // Using average of 30 days per month
				_ => value
			};
		}

		private decimal GetReorderLevel()
		{
			if (!decimal.TryParse(MaxUsageInput.Text, out decimal maxUsage) || maxUsage <= 0 ||
				!decimal.TryParse(MaxLeadTimeInput.Text, out decimal maxLeadTime) || maxLeadTime <= 0)
				return 0;

			// Calculate directly without conversion since both are in the same period
			return maxUsage * maxLeadTime;
		}

		#endregion

		#region Phase 5: EOQ Calculation

		private void CalculateEOQ_Click(object sender, RoutedEventArgs e)
		{
			// Validate inputs
			decimal annualDemand = GetAnnualDemand();
			if (annualDemand <= 0)
			{
				MessageBox.Show("Please enter a valid demand quantity.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				DemandQuantityInput.Focus();
				return;
			}

			decimal orderingCost = OrderingCosts.Sum(c => c.Cost);
			if (orderingCost <= 0)
			{
				MessageBox.Show("Please add at least one ordering cost.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			decimal holdingCost = HoldingCosts.Sum(c => c.Cost);
			if (holdingCost <= 0)
			{
				MessageBox.Show("Please add at least one holding cost.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Calculate EOQ using the formula: EOQ = √(2 × D × S / H)
			// D = Annual Demand
			// S = Ordering Cost per Order
			// H = Holding Cost per Unit per Year
			double eoq = Math.Sqrt((double)(2 * annualDemand * orderingCost / holdingCost));

			// Get reorder level (optional)
			decimal reorderLevel = GetReorderLevel();

			// Display results
			DisplayResults(eoq, annualDemand, orderingCost, holdingCost, reorderLevel);
		}

		private void DisplayResults(double eoq, decimal annualDemand, decimal orderingCost, decimal holdingCost, decimal reorderLevel)
		{
			// Show results panel
			ResultsPanel.Visibility = Visibility.Visible;

			// Display EOQ
			EOQResultDisplay.Text = $"{Math.Round(eoq):N0}";

			// Display formula with values
			FormulaValuesDisplay.Text = $"EOQ = √(2 × {annualDemand:N0} × {orderingCost:N2} / {holdingCost:N2})\n" +
										$"EOQ = √({2 * annualDemand * orderingCost / holdingCost:N2})\n" +
										$"EOQ = {Math.Round(eoq):N0} units";

			// Display reorder level if available
			if (reorderLevel > 0)
			{
				ReorderLevelResultPanel.Visibility = Visibility.Visible;
				ReorderLevelResultDisplay.Text = $"{reorderLevel:N0}";

				decimal maxUsage = decimal.TryParse(MaxUsageInput.Text, out decimal usage) ? usage : 0;
				decimal maxLeadTime = decimal.TryParse(MaxLeadTimeInput.Text, out decimal leadTime) ? leadTime : 0;
				string period = (LeadTimePeriodInput.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Days";
				string periodLower = period.ToLower();
				string periodSingular = period == "Days" ? "day" : period == "Weeks" ? "week" : "month";

				ReorderFormulaResultDisplay.Text = $"Reorder Level = {maxUsage:N0} units/{periodSingular} × {maxLeadTime:N0} {periodLower} = {reorderLevel:N0} units";
			}
			else
			{
				ReorderLevelResultPanel.Visibility = Visibility.Collapsed;
			}

			// Calculate and display additional insights
			DisplayInsights(eoq, annualDemand, orderingCost, holdingCost, reorderLevel);

			// Scroll to results
			ResultsPanel.BringIntoView();
		}

		private void DisplayInsights(double eoq, decimal annualDemand, decimal orderingCost, decimal holdingCost, decimal reorderLevel)
		{
			InsightsPanel.Children.Clear();

			// Number of orders per year
			double ordersPerYear = (double)annualDemand / eoq;
			AddInsight($"• Number of orders per year: {Math.Round(ordersPerYear, 1):N1}");

			// Time between orders (in days)
			double daysBetweenOrders = 365 / ordersPerYear;
			AddInsight($"• Days between orders: {Math.Round(daysBetweenOrders, 0):N0} days");

			// Total ordering cost per year
			decimal totalOrderingCostPerYear = orderingCost * (decimal)ordersPerYear;
			AddInsight($"• Total ordering cost per year: R {totalOrderingCostPerYear:N2}");

			// Average inventory level
			double avgInventory = eoq / 2;
			AddInsight($"• Average inventory level: {Math.Round(avgInventory, 0):N0} units");

			// Total holding cost per year
			decimal totalHoldingCostPerYear = holdingCost * (decimal)avgInventory;
			AddInsight($"• Total holding cost per year: R {totalHoldingCostPerYear:N2}");

			// Total inventory cost per year
			decimal totalInventoryCost = totalOrderingCostPerYear + totalHoldingCostPerYear;
			AddInsight($"• Total inventory cost per year: R {totalInventoryCost:N2}");

			// Add reorder level insight if available
			if (reorderLevel > 0)
			{
				AddInsight("");
				AddInsight($"• Place new order when inventory reaches: {reorderLevel:N0} units");

				// Calculate safety stock suggestion
				double safetyMargin = (double)reorderLevel - avgInventory;
				if (safetyMargin > 0)
				{
					AddInsight($"• Safety stock buffer: {Math.Round(safetyMargin, 0):N0} units above average inventory");
				}

				// Calculate period coverage at reorder level
				if (decimal.TryParse(MaxUsageInput.Text, out decimal maxUsage) && maxUsage > 0 &&
					decimal.TryParse(MaxLeadTimeInput.Text, out decimal maxLeadTime) && maxLeadTime > 0)
				{
					string period = (LeadTimePeriodInput.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Days";
					string periodLower = period.ToLower();
					AddInsight($"• Reorder level represents: {maxLeadTime:N1} {periodLower} of maximum usage");
				}
			}
		}

		private void AddInsight(string text)
		{
			var textBlock = new TextBlock
			{
				Text = text,
				FontSize = 13,
				Margin = new Thickness(0, 3, 0, 3),
				TextWrapping = TextWrapping.Wrap
			};
			InsightsPanel.Children.Add(textBlock);
		}

		#endregion
	}

	public class CostItem : INotifyPropertyChanged
	{
		private string _description;
		private decimal _cost;

		public string Description
		{
			get => _description;
			set
			{
				if (_description != value)
				{
					_description = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal Cost
		{
			get => _cost;
			set
			{
				if (_cost != value)
				{
					_cost = value;
					OnPropertyChanged();
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	// Dialog for adding costs
	public class CostInputDialog : Window
	{
		private TextBox descriptionInput;
		private TextBox costInput;

		public string CostDescription { get; private set; }
		public decimal CostAmount { get; private set; }

		public CostInputDialog(string costType)
		{
			Title = $"Add {costType}";
			Width = 400;
			Height = 200;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			ResizeMode = ResizeMode.NoResize;

			var grid = new Grid { Margin = new Thickness(20) };
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			// Description
			var descLabel = new TextBlock { Text = "Description:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 10, 5) };
			Grid.SetRow(descLabel, 0);
			Grid.SetColumn(descLabel, 0);
			grid.Children.Add(descLabel);

			descriptionInput = new TextBox { Margin = new Thickness(0, 5, 0, 5), Padding = new Thickness(5) };
			Grid.SetRow(descriptionInput, 0);
			Grid.SetColumn(descriptionInput, 1);
			grid.Children.Add(descriptionInput);

			// Cost
			var costLabel = new TextBlock { Text = "Cost (R):", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 10, 5) };
			Grid.SetRow(costLabel, 1);
			Grid.SetColumn(costLabel, 0);
			grid.Children.Add(costLabel);

			costInput = new TextBox { Margin = new Thickness(0, 5, 0, 5), Padding = new Thickness(5) };
			Grid.SetRow(costInput, 1);
			Grid.SetColumn(costInput, 1);
			grid.Children.Add(costInput);

			// Buttons
			var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
			Grid.SetRow(buttonPanel, 2);
			Grid.SetColumn(buttonPanel, 1);

			var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(5) };
			okButton.Click += OkButton_Click;
			buttonPanel.Children.Add(okButton);

			var cancelButton = new Button { Content = "Cancel", Width = 75, Padding = new Thickness(5) };
			cancelButton.Click += (s, e) => DialogResult = false;
			buttonPanel.Children.Add(cancelButton);

			grid.Children.Add(buttonPanel);

			Content = grid;

			descriptionInput.Focus();
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(descriptionInput.Text))
			{
				MessageBox.Show("Please enter a description.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				descriptionInput.Focus();
				return;
			}

			if (!decimal.TryParse(costInput.Text, out decimal cost) || cost <= 0)
			{
				MessageBox.Show("Please enter a valid cost amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				costInput.Focus();
				return;
			}

			CostDescription = descriptionInput.Text;
			CostAmount = cost;
			DialogResult = true;
		}
	}
}
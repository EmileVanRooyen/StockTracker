using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StockTracker.Views
{
	public partial class LabourCalculation : UserControl
	{
		public ObservableCollection<SalesBudgetRow> SalesBudgetItems { get; set; }

		public LabourCalculation()
		{
			InitializeComponent();
			SalesBudgetItems = new ObservableCollection<SalesBudgetRow>();
			LabourDataGrid.ItemsSource = SalesBudgetItems;

			// Subscribe to collection changes
			SalesBudgetItems.CollectionChanged += (s, e) =>
			{
				UpdateTotalSales();
				RefreshProductivityInputs();
			};
		}

		#region Phase 1: Sales Budget

		private void AddRow_Click(object sender, RoutedEventArgs e)
		{
			InputPanel.Visibility = Visibility.Visible;
			ClearInputs_Click(null, null);
			ProductNameInput.Focus();
		}

		private void RemoveRow_Click(object sender, RoutedEventArgs e)
		{
			if (LabourDataGrid.SelectedItem is SalesBudgetRow selectedRow)
			{
				SalesBudgetItems.Remove(selectedRow);
				UpdateTotalSales();
			}
		}

		private void ViewSummary_Click(object sender, RoutedEventArgs e)
		{
			InputPanel.Visibility = InputPanel.Visibility == Visibility.Visible
				? Visibility.Collapsed
				: Visibility.Visible;
		}

		private void PriceInput_TextChanged(object sender, TextChangedEventArgs e)
		{
			CalculateTotal();
		}

		private void CalculateTotal()
		{
			// Null check to prevent errors during initialization
			if (PricePerUnitCalculated == null || CalculatedTotalDisplay == null)
				return;

			decimal pricePerUnit = 0;

			// Calculate price per unit from bulk pricing
			if (decimal.TryParse(BulkPriceInput?.Text, out decimal bulkPrice) &&
				decimal.TryParse(BulkUnitsInput?.Text, out decimal bulkUnits) &&
				bulkUnits > 0)
			{
				pricePerUnit = bulkPrice / bulkUnits;
				PricePerUnitCalculated.Text = $"Price per unit: R {pricePerUnit:N2}";
			}
			else
			{
				PricePerUnitCalculated.Text = "Price per unit: R 0.00";
			}

			// Calculate total sales
			if (decimal.TryParse(UnitsQuantityInput?.Text, out decimal units) && pricePerUnit > 0)
			{
				decimal total = units * pricePerUnit;
				CalculatedTotalDisplay.Text = $"R {total:N2}";
			}
			else
			{
				CalculatedTotalDisplay.Text = "R 0.00";
			}
		}

		private void AddToTable_Click(object sender, RoutedEventArgs e)
		{
			// Validate inputs
			if (string.IsNullOrWhiteSpace(ProductNameInput.Text))
			{
				MessageBox.Show("Please enter a product name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				ProductNameInput.Focus();
				return;
			}

			if (!decimal.TryParse(UnitsQuantityInput.Text, out decimal units) || units <= 0)
			{
				MessageBox.Show("Please enter a valid quantity for units.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				UnitsQuantityInput.Focus();
				return;
			}

			if (!decimal.TryParse(BulkPriceInput.Text, out decimal bulkPrice) || bulkPrice <= 0)
			{
				MessageBox.Show("Please enter a valid bulk price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				BulkPriceInput.Focus();
				return;
			}

			if (!decimal.TryParse(BulkUnitsInput.Text, out decimal bulkUnits) || bulkUnits <= 0)
			{
				MessageBox.Show("Please enter a valid number of units for the bulk price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				BulkUnitsInput.Focus();
				return;
			}

			// Get unit type
			string unitType = (UnitsTypeInput.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "units";

			// Calculate price per unit
			decimal pricePerUnit = bulkPrice / bulkUnits;

			// Calculate total
			decimal totalSales = units * pricePerUnit;

			// Create new row
			var newRow = new SalesBudgetRow
			{
				ProductName = ProductNameInput.Text,
				UnitsQuantity = units,
				UnitsType = unitType,
				PricePerUnit = pricePerUnit,
				TotalSales = totalSales
			};

			SalesBudgetItems.Add(newRow);
			UpdateTotalSales();

			// Clear inputs
			ClearInputs_Click(null, null);
			ProductNameInput.Focus();
		}

		private void ClearInputs_Click(object sender, RoutedEventArgs e)
		{
			ProductNameInput.Clear();
			UnitsQuantityInput.Clear();
			BulkPriceInput.Clear();
			BulkUnitsInput.Text = "1";
			UnitsTypeInput.SelectedIndex = 0;

			if (CalculatedTotalDisplay != null)
				CalculatedTotalDisplay.Text = "R 0.00";

			if (PricePerUnitCalculated != null)
				PricePerUnitCalculated.Text = "Price per unit: R 0.00";
		}

		private void UpdateTotalSales()
		{
			if (TotalSalesText == null)
				return;

			decimal totalSales = SalesBudgetItems.Sum(item => item.TotalSales);
			TotalSalesText.Text = totalSales.ToString("N0");
		}

		#endregion

		#region Phase 2: Direct Labour Budget

		private void RefreshProductivityInputs()
		{
			if (ProductivityInputPanel == null)
				return;

			ProductivityInputPanel.Children.Clear();

			if (SalesBudgetItems.Count == 0)
			{
				var noProductsText = new TextBlock
				{
					Text = "No products added yet. Please add products in Phase 1 first.",
					FontStyle = FontStyles.Italic,
					Foreground = Brushes.Gray,
					Margin = new Thickness(0, 10, 0, 10)
				};
				ProductivityInputPanel.Children.Add(noProductsText);
				return;
			}

			foreach (var product in SalesBudgetItems)
			{
				var outerPanel = new StackPanel
				{
					Margin = new Thickness(0, 5, 0, 15)
				};

				// Product header
				var headerPanel = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 0, 0, 5)
				};

				var productHeader = new TextBlock
				{
					Text = product.ProductName,
					FontWeight = FontWeights.Bold,
					FontSize = 14,
					Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210))
				};

				headerPanel.Children.Add(productHeader);
				outerPanel.Children.Add(headerPanel);

				// Productivity rate input
				var productivityPanel = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(20, 0, 0, 5)
				};

				var productivityLabel = new TextBlock
				{
					Text = "Productivity Rate:",
					Width = 140,
					VerticalAlignment = VerticalAlignment.Center
				};

				var productivityInput = new TextBox
				{
					Width = 100,
					Padding = new Thickness(5),
					Margin = new Thickness(5, 0, 5, 0),
					Tag = product,
					Text = product.ProductivityRate > 0 ? product.ProductivityRate.ToString() : ""
				};

				productivityInput.TextChanged += (s, e) =>
				{
					if (decimal.TryParse(productivityInput.Text, out decimal rate))
					{
						product.ProductivityRate = rate;
					}
				};

				var unitLabel = new TextBlock
				{
					Text = $"{product.UnitsType} per hour",
					VerticalAlignment = VerticalAlignment.Center
				};

				productivityPanel.Children.Add(productivityLabel);
				productivityPanel.Children.Add(productivityInput);
				productivityPanel.Children.Add(unitLabel);

				outerPanel.Children.Add(productivityPanel);

				// Wage rate input
				var wagePanel = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(20, 0, 0, 0)
				};

				var wageLabel = new TextBlock
				{
					Text = "Wage Rate:",
					Width = 140,
					VerticalAlignment = VerticalAlignment.Center
				};

				var wageInput = new TextBox
				{
					Width = 100,
					Padding = new Thickness(5),
					Margin = new Thickness(5, 0, 5, 0),
					Tag = product,
					Text = product.WageRate > 0 ? product.WageRate.ToString() : ""
				};

				wageInput.TextChanged += (s, e) =>
				{
					if (decimal.TryParse(wageInput.Text, out decimal wage))
					{
						product.WageRate = wage;
					}
				};

				var wageUnitLabel = new TextBlock
				{
					Text = "R per hour",
					VerticalAlignment = VerticalAlignment.Center
				};

				wagePanel.Children.Add(wageLabel);
				wagePanel.Children.Add(wageInput);
				wagePanel.Children.Add(wageUnitLabel);

				outerPanel.Children.Add(wagePanel);

				// Add separator
				var separator = new Separator
				{
					Margin = new Thickness(0, 10, 0, 0),
					Background = new SolidColorBrush(Color.FromRgb(220, 220, 220))
				};
				outerPanel.Children.Add(separator);

				ProductivityInputPanel.Children.Add(outerPanel);
			}
		}

		private void CalculateLabourBudget_Click(object sender, RoutedEventArgs e)
		{
			if (SalesBudgetItems.Count == 0)
			{
				MessageBox.Show("Please add products in Phase 1 first.", "No Products", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			// Validate productivity rates and wage rates
			foreach (var product in SalesBudgetItems)
			{
				if (product.ProductivityRate <= 0)
				{
					MessageBox.Show($"Please enter a productivity rate for {product.ProductName}.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				if (product.WageRate <= 0)
				{
					MessageBox.Show($"Please enter a wage rate for {product.ProductName}.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}

			// Calculate labour hours and costs for each product
			foreach (var product in SalesBudgetItems)
			{
				product.LabourHours = product.UnitsQuantity / product.ProductivityRate;
				product.LabourCost = product.LabourHours * product.WageRate;
			}

			// Build the labour budget table
			BuildLabourBudgetTable();
		}

		private void BuildLabourBudgetTable()
		{
			if (LabourBudgetTable == null)
				return;

			LabourBudgetTable.Children.Clear();
			LabourBudgetTable.ColumnDefinitions.Clear();
			LabourBudgetTable.RowDefinitions.Clear();

			// Define row definitions
			for (int i = 0; i < 6; i++)
			{
				LabourBudgetTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			}

			// Define column definitions: First column + one for each product
			LabourBudgetTable.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
			foreach (var product in SalesBudgetItems)
			{
				LabourBudgetTable.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			}

			// Row 0: Headers
			AddTableCell(0, 0, "", "#F5F5F5", true);
			int colIndex = 1;
			foreach (var product in SalesBudgetItems)
			{
				AddTableCell(0, colIndex, $"{product.UnitsType} ({product.ProductName})", "#F5F5F5", true);
				colIndex++;
			}

			// Row 1: Production units
			AddTableCell(1, 0, "Production units", "#FFFFFF", true);
			colIndex = 1;
			foreach (var product in SalesBudgetItems)
			{
				AddTableCell(1, colIndex, product.UnitsQuantity.ToString("N0"), "#FFFFFF");
				colIndex++;
			}

			// Row 2: X Labour Hours
			AddTableCell(2, 0, "X Labour Hours", "#FFFFFF", true);
			colIndex = 1;
			foreach (var product in SalesBudgetItems)
			{
				AddTableCell(2, colIndex, product.LabourHours.ToString("N2"), "#FFFFFF");
				colIndex++;
			}

			// Row 3: Total (Labour Hours)
			AddTableCell(3, 0, "Total", "#FFFFFF", true);
			colIndex = 1;
			decimal totalLabourHours = 0;
			foreach (var product in SalesBudgetItems)
			{
				totalLabourHours += product.LabourHours;
				AddTableCell(3, colIndex, product.LabourHours.ToString("N2"), "#FFFFFF");
				colIndex++;
			}

			// Row 4: Wage Rate
			AddTableCell(4, 0, "Wage Rate", "#FFFFFF", true);
			colIndex = 1;
			foreach (var product in SalesBudgetItems)
			{
				AddTableCell(4, colIndex, $"R {product.WageRate:N2}", "#FFFFFF");
				colIndex++;
			}

			// Row 5: Total (Labour Cost)
			AddTableCell(5, 0, "Total", "#2C2C2C", true, true);
			colIndex = 1;
			decimal totalLabourCost = 0;
			foreach (var product in SalesBudgetItems)
			{
				totalLabourCost += product.LabourCost;
				AddTableCell(5, colIndex, $"R {product.LabourCost:N2}", "#2C2C2C", false, true);
				colIndex++;
			}
		}

		private void AddTableCell(int row, int col, string text, string bgColor, bool isBold = false, bool isTotal = false)
		{
			var border = new Border
			{
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1),
				Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
				Padding = new Thickness(10, 8, 10, 8)
			};

			var textBlock = new TextBlock
			{
				Text = text,
				FontSize = 13,
				HorizontalAlignment = col == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center
			};

			if (isBold)
			{
				textBlock.FontWeight = FontWeights.Bold;
			}

			if (isTotal)
			{
				textBlock.Foreground = Brushes.White;
				textBlock.FontWeight = FontWeights.Bold;
			}

			border.Child = textBlock;

			Grid.SetRow(border, row);
			Grid.SetColumn(border, col);

			LabourBudgetTable.Children.Add(border);
		}

		#endregion
	}

	public class SalesBudgetRow : INotifyPropertyChanged
	{
		private string _productName;
		private decimal _unitsQuantity;
		private string _unitsType;
		private decimal _pricePerUnit;
		private decimal _totalSales;
		private decimal _productivityRate;
		private decimal _wageRate;
		private decimal _labourHours;
		private decimal _labourCost;

		public string ProductName
		{
			get => _productName;
			set
			{
				if (_productName != value)
				{
					_productName = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal UnitsQuantity
		{
			get => _unitsQuantity;
			set
			{
				if (_unitsQuantity != value)
				{
					_unitsQuantity = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(UnitsDisplay));
				}
			}
		}

		public string UnitsType
		{
			get => _unitsType;
			set
			{
				if (_unitsType != value)
				{
					_unitsType = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(UnitsDisplay));
				}
			}
		}

		public decimal PricePerUnit
		{
			get => _pricePerUnit;
			set
			{
				if (_pricePerUnit != value)
				{
					_pricePerUnit = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(PricePerUnitDisplay));
				}
			}
		}

		public decimal TotalSales
		{
			get => _totalSales;
			set
			{
				if (_totalSales != value)
				{
					_totalSales = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(TotalSalesDisplay));
				}
			}
		}

		public decimal ProductivityRate
		{
			get => _productivityRate;
			set
			{
				if (_productivityRate != value)
				{
					_productivityRate = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal WageRate
		{
			get => _wageRate;
			set
			{
				if (_wageRate != value)
				{
					_wageRate = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal LabourHours
		{
			get => _labourHours;
			set
			{
				if (_labourHours != value)
				{
					_labourHours = value;
					OnPropertyChanged();
				}
			}
		}

		public decimal LabourCost
		{
			get => _labourCost;
			set
			{
				if (_labourCost != value)
				{
					_labourCost = value;
					OnPropertyChanged();
				}
			}
		}

		// Display properties
		public string UnitsDisplay => $"{UnitsQuantity:N0} {UnitsType}";
		public string PricePerUnitDisplay => $"R{PricePerUnit:N2}";
		public string TotalSalesDisplay => TotalSales.ToString("N0");

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
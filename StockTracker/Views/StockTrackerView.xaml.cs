using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace StockTracker.Views
{
	public partial class StockTrackerView : UserControl
	{
		private List<StockBatch> stockBatches;
		private ObservableCollection<TransactionRow> transactions;
		private int transactionCounter;
		private int batchIdCounter;

		public StockTrackerView()
		{
			InitializeComponent();
			InitializeStockTracker();
		}

		private void InitializeStockTracker()
		{
			stockBatches = new List<StockBatch>();
			transactions = new ObservableCollection<TransactionRow>();
			transactionCounter = 0;
			batchIdCounter = 1;

			// Set default dates to today
			dpOpeningDate.SelectedDate = DateTime.Now;
			dpPurchaseDate.SelectedDate = DateTime.Now;
			dpReturnDate.SelectedDate = DateTime.Now;
			dpSellDate.SelectedDate = DateTime.Now;

			dgTransactions.ItemsSource = transactions;

			UpdateSummary();
			RefreshReturnBatchComboBox();
		}

		private void RefreshReturnBatchComboBox()
		{
			cmbReturnBatch.ItemsSource = null;
			cmbReturnBatch.ItemsSource = stockBatches.Where(b => b.Units > 0).ToList();

			if (cmbReturnBatch.Items.Count > 0)
			{
				cmbReturnBatch.SelectedIndex = 0;
			}
			else
			{
				txtReturnBatchInfo.Text = "No stock batches available";
			}
		}

		private void CmbReturnBatch_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cmbReturnBatch.SelectedItem is StockBatch batch)
			{
				txtReturnBatchInfo.Text = $"Available: {batch.Units:N2} units @ R{batch.PricePerUnit:N2} (Total: R{batch.Units * batch.PricePerUnit:N2})";
			}
			else
			{
				txtReturnBatchInfo.Text = "";
			}
		}

		private void BtnAddOpening_Click(object sender, RoutedEventArgs e)
		{
			if (!dpOpeningDate.SelectedDate.HasValue)
			{
				MessageBox.Show("Please select a date.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(txtOpeningUnits.Text, out decimal units) || units <= 0)
			{
				MessageBox.Show("Please enter a valid number of units.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(txtOpeningPrice.Text, out decimal price) || price <= 0)
			{
				MessageBox.Show("Please enter a valid price per unit.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DateTime date = dpOpeningDate.SelectedDate.Value;
			var batch = new StockBatch(batchIdCounter++, units, price, date, "Opening Balance");
			stockBatches.Add(batch);

			AddTransaction(date, units, price, 0, null, "Opening Balance", null);

			txtOpeningUnits.Clear();
			txtOpeningPrice.Clear();
			UpdateSummary();
			RefreshReturnBatchComboBox();
		}

		private void BtnPurchase_Click(object sender, RoutedEventArgs e)
		{
			if (!dpPurchaseDate.SelectedDate.HasValue)
			{
				MessageBox.Show("Please select a date.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(txtPurchaseUnits.Text, out decimal units) || units <= 0)
			{
				MessageBox.Show("Please enter a valid number of units.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(txtPurchasePrice.Text, out decimal price) || price <= 0)
			{
				MessageBox.Show("Please enter a valid price per unit.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DateTime date = dpPurchaseDate.SelectedDate.Value;
			var batch = new StockBatch(batchIdCounter++, units, price, date, "Purchase");
			stockBatches.Add(batch);

			AddTransaction(date, units, price, 0, null, $"Purchase - Batch #{batch.BatchId}", null);

			txtPurchaseUnits.Clear();
			txtPurchasePrice.Clear();
			UpdateSummary();
			RefreshReturnBatchComboBox();
		}

		private void BtnReturn_Click(object sender, RoutedEventArgs e)
		{
			if (!dpReturnDate.SelectedDate.HasValue)
			{
				MessageBox.Show("Please select a date.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (cmbReturnBatch.SelectedItem is not StockBatch selectedBatch)
			{
				MessageBox.Show("Please select a batch to return from.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(txtReturnUnits.Text, out decimal unitsToReturn) || unitsToReturn <= 0)
			{
				MessageBox.Show("Please enter a valid number of units to return.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (unitsToReturn > selectedBatch.Units)
			{
				MessageBox.Show($"Cannot return {unitsToReturn:N2} units. Only {selectedBatch.Units:N2} units available in this batch.",
					"Insufficient Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DateTime date = dpReturnDate.SelectedDate.Value;

			selectedBatch.Units -= unitsToReturn;

			var batchBreakdown = new List<BatchAllocation>
			{
				new BatchAllocation(selectedBatch.BatchId, unitsToReturn, selectedBatch.PricePerUnit)
			};

			AddTransaction(date, 0, null, unitsToReturn, selectedBatch.PricePerUnit, $"Return - Batch #{selectedBatch.BatchId}", batchBreakdown);

			txtReturnUnits.Clear();
			UpdateSummary();
			RefreshReturnBatchComboBox();
		}

		private void BtnSell_Click(object sender, RoutedEventArgs e)
		{
			if (!dpSellDate.SelectedDate.HasValue)
			{
				MessageBox.Show("Please select a date.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(txtSellUnits.Text, out decimal unitsToSell) || unitsToSell <= 0)
			{
				MessageBox.Show("Please enter a valid number of units to sell.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Selling price is now optional
			decimal? sellingPrice = null;
			if (!string.IsNullOrWhiteSpace(txtSellingPrice.Text))
			{
				if (decimal.TryParse(txtSellingPrice.Text, out decimal parsedPrice) && parsedPrice > 0)
				{
					sellingPrice = parsedPrice;
				}
				else
				{
					MessageBox.Show("Please enter a valid selling price or leave it empty.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}

			decimal totalUnits = stockBatches.Sum(b => b.Units);
			if (unitsToSell > totalUnits)
			{
				MessageBox.Show($"Cannot sell {unitsToSell:N2} units. Only {totalUnits:N2} units available.",
					"Insufficient Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DateTime date = dpSellDate.SelectedDate.Value;
			decimal remainingToSell = unitsToSell;
			decimal totalCost = 0;
			var batchesUsed = new List<string>();
			var batchBreakdown = new List<BatchAllocation>();

			// FIFO - Process batches in order
			foreach (var batch in stockBatches.Where(b => b.Units > 0).OrderBy(b => b.BatchId))
			{
				if (remainingToSell <= 0) break;

				if (batch.Units <= remainingToSell)
				{
					// Sell entire batch
					totalCost += batch.Units * batch.PricePerUnit;
					batchesUsed.Add($"#{batch.BatchId}");
					batchBreakdown.Add(new BatchAllocation(batch.BatchId, batch.Units, batch.PricePerUnit));
					remainingToSell -= batch.Units;
					batch.Units = 0;
				}
				else
				{
					// Partial sale from batch
					totalCost += remainingToSell * batch.PricePerUnit;
					batchesUsed.Add($"#{batch.BatchId}");
					batchBreakdown.Add(new BatchAllocation(batch.BatchId, remainingToSell, batch.PricePerUnit));
					batch.Units -= remainingToSell;
					remainingToSell = 0;
				}
			}

			decimal averageCost = unitsToSell > 0 ? totalCost / unitsToSell : 0;
			string transactionType = sellingPrice.HasValue ?
				$"Sale - Batches: {string.Join(", ", batchesUsed)}" :
				$"Issue - Batches: {string.Join(", ", batchesUsed)}";

			AddTransaction(date, 0, null, unitsToSell, sellingPrice ?? averageCost, transactionType, batchBreakdown);

			txtSellUnits.Clear();
			txtSellingPrice.Clear();
			UpdateSummary();
			RefreshReturnBatchComboBox();
		}

		private void BtnStartOver_Click(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Are you sure you want to clear all data and start over?",
				"Confirm Start Over", MessageBoxButton.YesNo, MessageBoxImage.Question);

			if (result == MessageBoxResult.Yes)
			{
				InitializeStockTracker();
				MessageBox.Show("All data has been cleared.", "Start Over", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void AddTransaction(DateTime date, decimal receivedUnits, decimal? receivedPrice,
			decimal issuedUnits, decimal? issuedPrice, string transactionType, List<BatchAllocation> batchBreakdown)
		{
			decimal totalUnits = stockBatches.Sum(b => b.Units);
			decimal totalValue = stockBatches.Sum(b => b.Units * b.PricePerUnit);
			decimal averagePrice = totalUnits > 0 ? totalValue / totalUnits : 0;

			// Format issued price to show batch breakdown
			string issuedPriceDisplay = "";
			string issuedUnitsDisplay = "";

			if (issuedUnits > 0)
			{
				issuedUnitsDisplay = issuedUnits.ToString("N2");

				if (batchBreakdown != null && batchBreakdown.Count > 0)
				{
					if (batchBreakdown.Count == 1)
					{
						// Single batch - show simple price
						issuedPriceDisplay = $"R{batchBreakdown[0].PricePerUnit:N2}";
					}
					else
					{
						// Multiple batches - show detailed breakdown
						var breakdownParts = batchBreakdown.Select(b =>
							$"{b.Units:N2} @ R{b.PricePerUnit:N2}");
						issuedPriceDisplay = string.Join("; ", breakdownParts);
					}
				}
				else if (issuedPrice.HasValue)
				{
					issuedPriceDisplay = $"R{issuedPrice.Value:N2}";
				}
			}

			var transaction = new TransactionRow
			{
				Date = date,
				ReceivedUnits = receivedUnits > 0 ? receivedUnits.ToString("N2") : "",
				ReceivedPrice = receivedPrice.HasValue ? $"R{receivedPrice.Value:N2}" : "",
				ReceivedAmount = receivedUnits > 0 && receivedPrice.HasValue ? $"R{(receivedUnits * receivedPrice.Value):N2}" : "",
				IssuedUnits = issuedUnitsDisplay,
				IssuedPrice = issuedPriceDisplay,
				IssuedAmount = issuedUnits > 0 && batchBreakdown != null && batchBreakdown.Count > 0 ?
					$"R{batchBreakdown.Sum(b => b.Units * b.PricePerUnit):N2}" :
					(issuedUnits > 0 && issuedPrice.HasValue ? $"R{(issuedUnits * issuedPrice.Value):N2}" : ""),
				BalanceUnits = totalUnits.ToString("N2"),
				BalancePrice = $"R{averagePrice:N2}",
				BalanceAmount = $"R{totalValue:N2}",
				TransactionType = transactionType
			};

			transactions.Add(transaction);
		}

		private void UpdateSummary()
		{
			decimal totalUnits = stockBatches.Sum(b => b.Units);
			decimal totalValue = stockBatches.Sum(b => b.Units * b.PricePerUnit);
			decimal averageCost = totalUnits > 0 ? totalValue / totalUnits : 0;
			int activeBatches = stockBatches.Count(b => b.Units > 0);

			txtCurrentUnits.Text = $"Total Units: {totalUnits:N2}";
			txtAverageCost.Text = $"Average Cost: R{averageCost:N2}";
			txtTotalBatches.Text = $"Active Batches: {activeBatches}";
		}

		private class BatchAllocation
		{
			public int BatchId { get; }
			public decimal Units { get; }
			public decimal PricePerUnit { get; }

			public BatchAllocation(int batchId, decimal units, decimal pricePerUnit)
			{
				BatchId = batchId;
				Units = units;
				PricePerUnit = pricePerUnit;
			}
		}

		private class StockBatch
		{
			public int BatchId { get; }
			public decimal Units { get; set; }
			public decimal PricePerUnit { get; }
			public DateTime Date { get; }
			public string Type { get; }
			public string DisplayText => $"Batch #{BatchId} - {Date:yyyy-MM-dd} - {Units:N2} units @ R{PricePerUnit:N2} ({Type})";

			public StockBatch(int batchId, decimal units, decimal pricePerUnit, DateTime date, string type)
			{
				BatchId = batchId;
				Units = units;
				PricePerUnit = pricePerUnit;
				Date = date;
				Type = type;
			}
		}

		private class TransactionRow
		{
			public DateTime Date { get; set; }
			public string ReceivedUnits { get; set; } = "";
			public string ReceivedPrice { get; set; } = "";
			public string ReceivedAmount { get; set; } = "";
			public string IssuedUnits { get; set; } = "";
			public string IssuedPrice { get; set; } = "";
			public string IssuedAmount { get; set; } = "";
			public string BalanceUnits { get; set; } = "";
			public string BalancePrice { get; set; } = "";
			public string BalanceAmount { get; set; } = "";
			public string TransactionType { get; set; } = "";
		}
	}
}
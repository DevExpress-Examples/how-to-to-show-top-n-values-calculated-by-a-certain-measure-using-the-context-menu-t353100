﻿using ContextMenuToShowTopN_Example.nwindDataSetTableAdapters;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.PivotGrid;
using DevExpress.Xpf.PivotGrid.Internal;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;

namespace ContextMenuToShowTopN_Example
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        nwindDataSet.ProductReportsDataTable table = new nwindDataSet.ProductReportsDataTable();
        ProductReportsTableAdapter tableAdapter = new ProductReportsTableAdapter();
        public MainWindow()
        {
            InitializeComponent();
            tableAdapter.Fill(table);
            InitializePivot();
        }

        private void InitializePivot()
        {
            pivotGridControl1.DataSource = table;
            pivotGridControl1.RetrieveFields();
            pivotGridControl1.Fields["CategoryName"].Area = FieldArea.RowArea;
            pivotGridControl1.Fields["ProductName"].Area = FieldArea.RowArea;
            pivotGridControl1.Fields["ProductSales"].Area = FieldArea.DataArea;
            pivotGridControl1.Fields["ShippedDate"].Area = FieldArea.ColumnArea;
            pivotGridControl1.Fields["ShippedDate"].GroupInterval = FieldGroupInterval.DateMonthYear;
            pivotGridControl1.Fields["ShippedDate"].DisplayFolder = "Date";
            pivotGridControl1.BestFit();
        }

        private void pivotGridControl1_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            FieldValueElement fvElement = e.TargetElement as FieldValueElement;
            if (fvElement == null) return;

            FieldValueItem valueItem = fvElement.ElementData as FieldValueItem;
            if (valueItem.IsLastLevelItem)
            {
                string itemCaption = string.Format("Top 5 Values in this {0}", valueItem.IsColumn ? "Column" : "Row");
                BarCheckItem item = new BarCheckItem { Content = itemCaption };
                if (IsTopFiveValuesApplied(valueItem))
                    item.IsChecked = true;
                item.CheckedChanged += Item_CheckedChanged;

                item.Tag = valueItem;
                e.Customizations.Add(new AddBarItemAction { Item = item });
            }
        }

        private void Item_CheckedChanged(object sender, ItemClickEventArgs e)
        {
            BarCheckItem item = sender as BarCheckItem;
            FieldValueItem elementData = e.Item.Tag as FieldValueItem;
            if ((bool)item.IsChecked)
                SetTopFiveValues(elementData);
            else
                ResetTopFiveValues(elementData.PivotGrid);
        }
        private static void SetTopFiveValues(FieldValueItem valueItem)
        {
            var sortConditions = GetConditions(valueItem);
            valueItem.PivotGrid.BeginUpdate();
            ResetTopFiveValues(valueItem.PivotGrid);
            valueItem.PivotGrid.GetFieldsByArea(valueItem.IsColumn ? FieldArea.RowArea : FieldArea.ColumnArea).ForEach(f => {
                f.SortOrder = FieldSortOrder.Descending;
                f.SortByField = valueItem.DataField;
                f.SortByConditions.Clear();
                f.SortByConditions.AddRange(sortConditions.Select(c => new SortByCondition(c.Key, c.Value)));
                f.TopValueCount = 5;
                f.TopValueShowOthers = true;
            });
            valueItem.PivotGrid.EndUpdate();
        }
        private static bool IsTopFiveValuesApplied(FieldValueItem valueItem)
        {
            var fields = valueItem.PivotGrid.GetFieldsByArea(valueItem.IsColumn ? FieldArea.RowArea : FieldArea.ColumnArea);
            if (fields.Count == 0)
                return false;
            var conditions = GetConditions(valueItem);
            foreach (PivotGridField f in fields)
            {
                if (f.TopValueCount != 5)
                    return false;
                if (conditions.Count != f.SortByConditions.Count)
                    return false;
                for (int i = 0; i < conditions.Count; i++)
                {
                    if (f.SortByConditions[i].Field != conditions[i].Key ||
                        f.SortByConditions[i].Value != conditions[i].Value)
                        return false;
                }
            }
            return true;
        }
        private static void ResetTopFiveValues(PivotGridControl pivotGrid)
        {
            pivotGrid.BeginUpdate();
            var fields = pivotGrid.GetFieldsByArea(FieldArea.ColumnArea).Union(pivotGrid.GetFieldsByArea(FieldArea.RowArea));
            foreach (var f in fields)
            {
                f.SortByField = null;
                f.SortByConditions.Clear();
                f.TopValueCount = 0;
                f.TopValueShowOthers = false;
            }
            pivotGrid.EndUpdate();
        }
        private static List<KeyValuePair<PivotGridField, object>> GetConditions(FieldValueItem valueItem)
        {
            var fields = valueItem.PivotGrid.GetFieldsByArea(valueItem.IsColumn ? FieldArea.ColumnArea : FieldArea.RowArea).Where(f => f.AreaIndex <= valueItem.Field.AreaIndex);
            return fields.
                Select(f => new KeyValuePair<PivotGridField, object>(f,
                    valueItem.PivotGrid.GetFieldValue(f, valueItem.MinLastLevelIndex)
                )).ToList();
        }


    }
}
﻿using DevExpress.Data.Filtering;
using DevExpress.Web.Mvc;
using Ganedata.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Web;
using System.Web.Mvc;

namespace WMS.CustomBindings
{
    public class ProducctListCustomBinding
    {

        private static IQueryable<object> GetProductDataset(int tenantId, int warehouseId)
        {
            var productServices = DependencyResolver.Current.GetService<IProductServices>();
            var transactions = productServices.GetAllProductMasterDetail(tenantId, warehouseId);
            return transactions;
        }

        public static void ProductGetData(GridViewCustomBindingGetDataArgs e, int tenantId, int warehouseId)
        {
            var transactions = GetProductDataset(tenantId, warehouseId);

            if (e.State.SortedColumns.Count() > 0)
            {

                string sortString = "";

                foreach (var column in e.State.SortedColumns)
                {
                    sortString += column.FieldName + " " + column.SortOrder;
                }
                transactions = transactions.OrderBy(sortString);
            }
            else
            {
                transactions = transactions.OrderBy("SKUCode");
            }


            if (e.FilterExpression != string.Empty)
            {
                CriteriaOperator op = CriteriaOperator.Parse(e.FilterExpression);

                string filterString = CriteriaToWhereClauseHelper.GetDynamicLinqWhere(op);

                transactions = transactions.Where(filterString);

            }

            transactions = transactions.Skip(e.StartDataRowIndex).Take(e.DataRowCount);



            e.Data = transactions.ToList();
        }

        public static void ProductGetDataRowCount(GridViewCustomBindingGetDataRowCountArgs e, int tenantId, int warehouseId)
        {

            var transactions = GetProductDataset(tenantId, warehouseId);

            if (e.State.SortedColumns.Count() > 0)
            {

                string sortString = "";

                foreach (var column in e.State.SortedColumns)
                {
                    sortString += column.FieldName + " " + column.SortOrder;
                }

                transactions = transactions.OrderBy(sortString);
            }
            else
            {
                transactions = transactions.OrderBy("SKUCode");
            }
            if (e.FilterExpression != string.Empty)
            {
                CriteriaOperator op = CriteriaOperator.Parse(e.FilterExpression);
                string filterString = CriteriaToWhereClauseHelper.GetDynamicLinqWhere(op);
                transactions = transactions.Where(filterString);
            }

            e.DataRowCount = transactions.Count();

        }


        public static GridViewModel CreateProductGridViewModel()
        {
            var viewModel = new GridViewModel();
            viewModel.KeyFieldName = "ProductId";
            viewModel.Columns.Add("Name");
            viewModel.Columns.Add("SKUCode");
            viewModel.Columns.Add("BarCode");
            viewModel.Columns.Add("BarCode2");
            viewModel.Columns.Add("Location");
            viewModel.Columns.Add("UOM");
            viewModel.Columns.Add("DepartmentName");
            viewModel.Columns.Add("Serialisable");
            viewModel.Columns.Add("IsRawMaterial");
            viewModel.Columns.Add("InStock");
            viewModel.Columns.Add("Property");

            viewModel.Pager.PageSize = 10;
            return viewModel;
        }
    }
}
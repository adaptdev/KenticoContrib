using System;
using System.Data;
using System.Text;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.FormControls;
using CMS.FormEngine;
using CMS.GlobalHelper;
using CMS.SettingsProvider;

namespace CMSFormControls.ManyToManyManager
{
    /// <summary>
    /// Allows for the management of many-to-many relationships with custom table data,
    /// persisted via a join table.
    /// </summary>
    public partial class ManyToManyManager : FormEngineUserControl
    {
        private DataClassInfo _relatedCustomTable;
        private DataClassInfo _joinCustomTable;

        public DataClassInfo RelatedCustomTable
        {
            get { return _relatedCustomTable ?? (_relatedCustomTable = GetCustomTable("RelatedCustomTable")); }
        }

        public DataClassInfo JoinCustomTable
        {
            get { return _joinCustomTable ?? (_joinCustomTable = GetCustomTable("JoinCustomTable")); }
        }

        public string RelatedDataNameField
        {
            get { return ValidationHelper.GetString(GetValue("RelatedDataNameField"), null); }
        }

        public string JoinTableLeftKey
        {
            get { return ValidationHelper.GetString(GetValue("JoinTableLeftKey"), null); }
        }

        public string JoinTableRightKey
        {
            get { return ValidationHelper.GetString(GetValue("JoinTableRightKey"), null); }
        }

        /// <summary>
        /// The ID of the object currently being edited by this control.
        /// </summary>
        protected int CurrentItemId
        {
            get
            {
                return Form.EditedObject.GetType() == typeof (TreeNode)
                           ? ((TreeNode) Form.EditedObject).DocumentID
                           : ((SimpleDataClass) Form.EditedObject).ID;
            }
        }

        /// <summary>
        /// This form control only uses the join table for persistence, but all form controls are
        /// still required to provide a value, so we simply return an empty string here.
        /// </summary>
        public override object Value
        {
            get { return string.Empty; }
            set
            {
            }
        }

        /// <summary>
        /// Indicates whether the form control settings are valid.
        /// </summary>
        protected bool ControlIsValid;

        protected override void OnInit(EventArgs e)
        {
            Form.OnAfterSave += FormOnAfterSave;

            ControlIsValid = VerifyFieldExists(RelatedCustomTable, RelatedDataNameField) &&
                             VerifyFieldExists(JoinCustomTable, JoinTableLeftKey) &&
                             VerifyFieldExists(JoinCustomTable, JoinTableRightKey);
        }

        protected void Page_Load(object sender, EventArgs args)
        {
            if (!ControlIsValid) return;

            if (list.Items.Count > 0) return;

            list.DataValueField = "ItemID";
            list.DataTextField = RelatedDataNameField;

            var dataSql = String.Format("SELECT * FROM {0}", RelatedCustomTable.ClassTableName);

            try
            {
                list.DataSource = ConnectionHelper.ExecuteQuery(dataSql, null, QueryTypeEnum.SQLQuery, false);
                list.DataBind();
            }
            catch (Exception e)
            {
                HandleError(e);
            }

            LoadSelectedItems();
        }

        private void LoadSelectedItems()
        {
            // A current item ID of 0 means item being edited and new and has not been persisted yet
            if (CurrentItemId == 0) return;

            var sql = String.Format("SELECT {0} FROM {1} WHERE {2} = @id", JoinTableRightKey, JoinCustomTable.ClassTableName, JoinTableLeftKey);
            var queryParams = new QueryDataParameters { new DataParameter("id", CurrentItemId) };

            try
            {
                var joinData = ConnectionHelper.ExecuteQuery(sql, queryParams, QueryTypeEnum.SQLQuery, false);

                if (DataHelper.DataSourceIsEmpty(joinData)) return;

                foreach (DataRow row in joinData.Tables[0].Rows)
                {
                    var key = row[JoinTableRightKey].ToString();
                    list.Items.FindByValue(key).Selected = true;
                }
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }

        private void FormOnAfterSave(object sender, EventArgs eventArgs)
        {
            SaveSelectedItems();
        }

        private void SaveSelectedItems()
        {
            // Build a SQL statement that will delete all existing relationships for this object from the join table
            var deleteSql = string.Format("DELETE FROM {0} WHERE {1} = @id", JoinCustomTable.ClassTableName, JoinTableLeftKey);
            var queryParams = new QueryDataParameters { new DataParameter("id", CurrentItemId) };
            
            var sqlString = new StringBuilder();
            sqlString.Append(deleteSql);

            // Build a SQL statement that will insert a new record into the join table for each selected item
            foreach (System.Web.UI.WebControls.ListItem listItem in list.Items)
            {
                if (listItem.Selected)
                {
                    var insert = string.Format("INSERT INTO {0} ({1}, {2}) VALUES ({3}, {4})",
                                               JoinCustomTable.ClassTableName, JoinTableLeftKey, JoinTableRightKey,
                                               CurrentItemId, listItem.Value);

                    sqlString.Append("\n");
                    sqlString.Append(insert);
                }
            }

            ConnectionHelper.ExecuteQuery(sqlString.ToString(), queryParams, QueryTypeEnum.SQLQuery, true);
        }

        private void HandleError(string message)
        {
            HandleError(new Exception(message));
        }

        private void HandleError(Exception exception)
        {
            var ctrlError = new FormControlError {InnerException = exception};
            Controls.Add(ctrlError);
            list.Visible = false;
        }

        private DataClassInfo GetCustomTable(string columnName)
        {
            var name = ValidationHelper.GetString(GetValue(columnName), null);
            var customTable = DataClassInfoProvider.GetDataClass(name);
            if (customTable == null || !customTable.ClassIsCustomTable)
            {
                throw new ArgumentException(string.Format("The data class '{0}' could not be found", name));
            }
            return customTable;
        }

        private bool VerifyFieldExists(DataClassInfo customTable, string column)
        {
            if (customTable == null) return false;

            var formInfo = new FormInfo(customTable.ClassFormDefinition);
            var columns = formInfo.GetColumnNames();

            if (!columns.Contains(column))
            {
                HandleError(string.Format("The custom table {0} does not contain the column '{1}'", customTable.ClassName, column));
                return false;
            }

            return true;
        }
    }
}
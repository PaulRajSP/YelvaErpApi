using System.Text.Json.Serialization;

namespace HoneyErpAPI.Common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PermissionEnum
    {
        // Dashboard
        ViewDashboard,

        // Orders & Sales
        ViewOrders,
        CreateOrder,
        EditOrder,
        CancelOrder,
        ViewSalesReports,

        // Customers
        ViewCustomers,
        ManageCustomers,

        // Products & Inventory
        ViewProducts,
        ManageProducts,
        ViewRawMaterials,
        ManageRawMaterials,
        ViewBatches,
        ManageBatches,

        // Purchases & Suppliers
        ViewPurchases,
        ManagePurchases,
        ViewSuppliers,
        ManageSuppliers,

        // Operations & Production
        ViewProduction,
        ManageProduction,
        ViewPacking,
        ManagePacking,
        ViewLogistics,
        ManageLogistics,

        // Finance & Expenses
        ViewAccounts,
        ManageAccounts,
        ViewExpenses,
        ManageExpenses,

        // Employees & Roles Administration
        ViewEmployees,
        ManageEmployees,
        ManageRolesPermissions
    }
}

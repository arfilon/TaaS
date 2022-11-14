namespace TaaS
{
    public class TenantNotFoundException : System.Exception
    {
        public TenantNotFoundException(string host)
            : base($"{host} is not defined as a tenant")
        {

        }
    }
}

namespace Session07.Es.CM.ApplicationServices.Customers.Dtoes
{
    public class CustomerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CustomerId { get; set; }

        public AddressDto Address { get; set; }
    }
}

using System.Text.Json;
using StronglyTypedUid;

namespace Test
{
    public record CustomerG(CustomerGuId Id, string Name);

    [StronglyTypedUid(asUlid: false)]
    public readonly partial record struct CustomerGuId
    {
    }
    [StronglyTypedUid(asUlid: false)]
    public readonly partial record struct SupplierGuId
    {
    }
    public record CustomerU(CustomerUlid Id, string Name);

    [StronglyTypedUid(asUlid: true)]
    public readonly partial record struct CustomerUlid
    {
    }
    [StronglyTypedUid(asUlid: true)]
    public readonly partial record struct SupplierUlid
    {
    }

    public class UnitTest1
    {

        [Fact]
        public void SameValuesAreEquals()
        {
            var guid = Guid.NewGuid();
            var ulid = Ulid.NewUlid();

            CustomerGuId customerguid1 = guid;
            CustomerGuId customerguid2 = guid;

            CustomerUlid customerulid1 = ulid;
            CustomerUlid customerulid2 = ulid;

            Assert.Equal(customerguid1, customerguid2);
            Assert.Equal(customerulid1, customerulid2);

        }

        [Fact]
        public void DifferentValuesAreNotEquals()
        {
            CustomerGuId customerguid1 = CustomerGuId.NewCustomerGuId();
            CustomerGuId customerguid2 = CustomerGuId.NewCustomerGuId();

            CustomerUlid customerulid1 = CustomerUlid.NewCustomerUlid();
            CustomerUlid customerulid2 = CustomerUlid.NewCustomerUlid();

            Assert.NotEqual(customerguid1, customerguid2);
            Assert.NotEqual(customerulid1, customerulid2);

        }
        [Fact]
        public void DifferentTypesAreNotEquals()
        {
            CustomerGuId customerguid1 = CustomerGuId.NewCustomerGuId();
            SupplierGuId supplierguid2 = SupplierGuId.NewSupplierGuId();

            CustomerUlid customerulid1 = CustomerUlid.NewCustomerUlid();
            SupplierUlid supplierulid2 = SupplierUlid.NewSupplierUlid();

            Assert.NotEqual(customerguid1.Value, supplierguid2.Value);
            Assert.NotEqual(customerulid1.Value, supplierulid2.Value);

        }
        [Fact]
        public void Serialization()
        {
            CustomerGuId customerGuid = new Guid("e2f7b687-e1bc-4644-8aae-2a44d17ef839");
            CustomerG customerg = new CustomerG(customerGuid, "John");

            CustomerUlid customerUlid = Ulid.Parse("01HV1GECPJZGQS9SDAVZG20M4S");
            CustomerU customeru = new CustomerU(customerUlid, "John");

            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = false
            };

            string jsonsg = JsonSerializer.Serialize(customerg, serializeOptions);
            string jsonsu = JsonSerializer.Serialize(customeru, serializeOptions);
            string jsong = "{\"Id\":\"e2f7b687-e1bc-4644-8aae-2a44d17ef839\",\"Name\":\"John\"}";
            string jsonu = "{\"Id\":\"01HV1GECPJZGQS9SDAVZG20M4S\",\"Name\":\"John\"}";

            Assert.Equal(jsonsg, jsong);
            Assert.Equal(jsonsu, jsonu);

        }

        [Fact]
        public void Deserialization()
        {
            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = false
            };

            string jsong = "{\"Id\":\"e2f7b687-e1bc-4644-8aae-2a44d17ef839\",\"Name\":\"John\"}";
            string jsonu = "{\"Id\":\"01HV1GECPJZGQS9SDAVZG20M4S\",\"Name\":\"John\"}";

            CustomerG? customerG = JsonSerializer.Deserialize<CustomerG>(jsong, serializeOptions);
            CustomerU? customerU = JsonSerializer.Deserialize<CustomerU>(jsonu, serializeOptions);

            string name = "John";
            Assert.Equal(customerG?.Name, name);
            Assert.Equal(customerU?.Name, name);
            Assert.Equal(customerG?.Id, Guid.Parse("e2f7b687-e1bc-4644-8aae-2a44d17ef839"));
            Assert.Equal(customerU?.Id, Ulid.Parse("01HV1GECPJZGQS9SDAVZG20M4S"));

        }
    }
}
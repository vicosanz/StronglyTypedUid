using System.ComponentModel;
using System.Text.Json;
using StronglyTypedUid;


Console.WriteLine("Hello, World!");

CustomerId customer = CustomerId.NewCustomerId();
var id = customer.ToString();
Console.WriteLine(customer);
if (CustomerId.TryParse(id, out CustomerId customer2))
{
    Console.WriteLine(customer2);
}
Console.WriteLine(customer == customer2);

var converter = TypeDescriptor.GetConverter(typeof(CustomerIdTypeConverter));
Console.WriteLine(converter.ConvertToString(customer2));

var serializeOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters =
    {
        //new CustomerIdJsonConverter()
    }
};

var newcustomer = new Customer(CustomerId.NewCustomerId(), "Jhon");
Console.WriteLine( JsonSerializer.Serialize(newcustomer, serializeOptions));

//var jsoncustomer = "{  \"Id\": \"01HV1GECPJZGQS9SDAVZG20M4S\",  \"Name\": \"Jhon\"}";
var jsoncustomer = "{  \"Id\": \"e2f7b687-e1bc-4644-8aae-2a44d17ef839\",  \"Name\": \"Jhon\"}";

var newcustomer2 = JsonSerializer.Deserialize<Customer>(jsoncustomer);
Console.WriteLine(newcustomer2?.Id);
Console.WriteLine(newcustomer2?.Name);

public record Customer(CustomerId Id, string Name);

[StronglyTypedUid(asUlid:false)]
public readonly partial record struct CustomerId
{
}

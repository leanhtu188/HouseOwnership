using Spectre.Console;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var graph = Import();
var app = new App(graph);
app.Run();

List<Person> Import()
{
    List<Person> graph = new();
    string path;
    do
    {
        path = AnsiConsole.Ask<string>("Enter file path: ");
        if (!File.Exists(path))
        {
            Console.WriteLine("File not found.");
            path = string.Empty;
        }
        if(!path.EndsWith(".txt"))
        {
            Console.WriteLine("File must be of type TXT.");
        }
    } while (string.IsNullOrEmpty(path));

    using var reader = new StreamReader(File.OpenRead(path));
    string? line = string.Empty;
    AnsiConsole.Status()
        .Start("Importing", ctx =>
        {
            while (!reader.EndOfStream)
            {
                try
                {
                    line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    var fields = line.Split("\t");
                    if (string.IsNullOrEmpty(fields[Fields.ID])) continue;

                    var person = new Person()
                    {
                        Id = fields[Fields.ID],
                        Gender = fields[Fields.GENDER] == "0" ? Gender.Male : Gender.Female,
                        Name = fields[Fields.FULL_NAME],
                        FatherName = fields[Fields.FATHER_NAME],
                        MotherName = fields[Fields.MOTHER_NAME],
                        HouseOwnerName = fields[Fields.NAME_OF_OWNER],
                        Town = fields[Fields.TOWN],
                        CardNumber = fields[Fields.CARD_NUMBER],
                        RelationshipWithHouseOwner = fields[Fields.RELATIONSHIP_WITH_HOUSE_OWNER]
                    };
                    if (!graph.Any(x => x.Id == person.Id)) graph.Add(person);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    Console.WriteLine($"Entry: {line}");
                }
            }

            ctx.Status("Building relationship graph");
            ctx.Refresh();

            foreach (var person in graph)
            {
                if (person.FatherName != null)
                {
                    var father = graph.FirstOrDefault(x => x.Name == person.FatherName);
                    if (father is null) continue;
                    person.Father = father;
                    father.Children.Add(person);
                }
                if (person.MotherName != null)
                {
                    var mother = graph.FirstOrDefault(x => x.Name == person.MotherName);
                    if (mother is null) continue;
                    person.Mother = mother;
                    mother.Children.Add(person);
                }
                if (person.Father != null && person.Mother != null)
                {
                    person.Father.Spouse = person.Mother;
                    person.Mother.Spouse = person.Father;
                }

                if (!string.IsNullOrEmpty(person.HouseOwnerName) &&
                    !string.IsNullOrEmpty(person.RelationshipWithHouseOwner))
                {
                    var relationship = person.RelationshipWithHouseOwner?.ToLower().Trim();
                    var other = graph.FirstOrDefault(x => x.Name == person.HouseOwnerName);
                    if (other == null) continue;
                    if (relationship == "vợ" || relationship == "chồng")
                    {
                        person.Spouse = other;
                        other.Spouse = person;
                    }
                    else if (relationship == "anh ruột"
                        || relationship == "em"
                        || relationship == "em gái"
                        || relationship == "em trai"
                        || relationship == "chị")
                    {
                        if (person.Father != null) other.Father = person.Father;
                        else if (other.Father != null) person.Father = other.Father;

                        if (person.Mother != null) other.Mother = person.Mother;
                        else if (other.Mother != null) person.Mother = other.Mother;
                    }
                    else if (relationship == "anh vợ")
                    {

                    }
                    else if (relationship == "bà nội")
                    {

                    }
                    else if (relationship == "bà ngoại")
                    {

                    }
                    else if (relationship == "bác")
                    {

                    }
                    else if (relationship == "bố")
                    {

                    }
                    else if (relationship == "bố chồng")
                    {

                    }
                    else if (relationship == "cháu")
                    {

                    }
                    else if (relationship == "cháu họ")
                    {

                    }
                    else if (relationship == "cháu ngoại")
                    {

                    }
                    else if (relationship == "cháu nội")
                    {

                    }
                    else if (relationship == "chị chồng")
                    {

                    }
                    else if (relationship == "con")
                    {
                        if (other.Gender == Gender.Male) person.Father = other;
                        else if (other.Gender == Gender.Female) person.Mother = other;
                    }
                    else if (relationship == "con dâu")
                    {

                    }
                    else if (relationship == "con nuôi")
                    {

                    }
                    else if (relationship == "con rể")
                    {

                    }
                    else if (relationship == "con vợ")
                    {

                    }
                    else if (relationship == "em dâu")
                    {

                    }
                    else if (relationship == "em họ")
                    {

                    }
                    else if (relationship == "mẹ chồng")
                    {

                    }
                    else if (relationship == "người ở nhờ")
                    {

                    }
                    else if (relationship == "ông ngoại")
                    {

                    }
                    else if (relationship == "ông nội")
                    {

                    }
                }
            }

        });
    Console.Clear();
    return graph;
}


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
                if(person.Father != null && person.Mother!= null) 
                {
                    person.Father.Spouse = person.Mother;
                    person.Mother.Spouse = person.Father;
                }

                if (!string.IsNullOrEmpty(person.HouseOwnerName) &&
                    (person.RelationshipWithHouseOwner?.ToLower() == "vợ"
                    || person.RelationshipWithHouseOwner?.ToLower() == "chồng"))
                {
                    var spouse = graph.FirstOrDefault(x => x.Name == person.HouseOwnerName);
                    if (spouse == null) continue;
                    person.Spouse = spouse;
                    spouse.Spouse = person;
                }
            }
        });
    Console.Clear();
    return graph;
}


using Spectre.Console;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var app = new App();    
app.Run();

class App
{
    private readonly List<Person> _people = new();
    private const string DELIMITER = "\t";
    private class Fields
    {
        public const int TOWN = 1;
        public const int HSHK = 2;
        public const int ID = 3;
        public const int FULL_NAME = 4;
        public const int FAMILY_NAME = 5;
        public const int GIVEN_NAME = 6;
        public const int DATE_OF_BIRTH = 7;
        public const int RELATIONSHIP_WITH_HOUSE_OWNER = 8;
        public const int GENDER = 9;
        public const int MOVEMENT = 10;
        public const int TIME = 11;
        public const int NEW_ADDRESS = 12;
        public const int RACE = 13;
        public const int RELIGION = 14;
        public const int CARD_NUMBER = 15;
        public const int UNKNOWN_1 = 16;
        public const int UNKNOWN_2 = 17;
        public const int RESIDENCE = 18;
        public const int PLACE_OF_BIRTH = 19;
        public const int FATHER_NAME = 20;
        public const int MOTHER_NAME = 21;
        public const int NAME_OF_OWNER = 22;
    }

    private bool _ended = false;

    public void Run()
    {
        Import();
        while(!_ended)
        {
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .AddChoices(new[] {
                    "Lookup Relationships",
                    "Quit"
                }));

            if (selection == "Quit") _ended = true;
            else if(selection == "Lookup Relationships")
            {
                LookupRelationship();
            } else if(selection == "Draw Family Tree")
            {
                DrawFamilyTree();
            }
        }
    }

    private void DrawFamilyTree()
    {
        string id = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter first person ID: ")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]ID not found[/red]")
                .Validate(id =>
                {
                    return _people.Any(x => x.Id == id);
                })
            );

        var startNode = _people.First(x => x.Id == id);



    }

    private void LookupRelationship()
    {
        string id1 = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter first person ID: ")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]ID not found[/red]")
                .Validate(id =>
                {
                    return _people.Any(x => x.Id  == id);
                })
            );

        string id2 = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter second person ID: ")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]ID not found[/red]")
                .Validate(id =>
                {
                    return _people.Any(x => x.Id == id) && id != id1;
                })
            );

        var startNode = _people.First(x => x.Id == id1);
        var endNode = _people.First(x => x.Id == id2);

        Queue<Person> unvisitedNodes = new();
        Dictionary<Person,Person?> currentPath = new();
        unvisitedNodes.Enqueue(startNode);

        Person? parentNode = null;

        while (unvisitedNodes.Any())
        {
            var visited = unvisitedNodes.Dequeue();

            currentPath[visited] = parentNode;

            if (visited == endNode) break;

            if (visited.Father != null && !currentPath.ContainsKey(visited.Father)) unvisitedNodes.Enqueue(visited.Father);
            if (visited.Mother != null && !currentPath.ContainsKey(visited.Mother)) unvisitedNodes.Enqueue(visited.Mother);
            if (visited.Spouse != null && !currentPath.ContainsKey(visited.Spouse)) unvisitedNodes.Enqueue(visited.Spouse);

            parentNode = visited;
        }

        if (!currentPath.ContainsKey(endNode))
        {
            Console.WriteLine($"{startNode.Name} and {endNode.Name} are not related.");
        } else
        {
            List<Person> mainPath = new();
            Person index = endNode;
            while(currentPath.ContainsKey(index!) && currentPath[index!] != null)
            {
                mainPath.Add(currentPath[index]!);
                index = currentPath[index]!;
            }

            mainPath.Reverse();
            Console.WriteLine(string.Join(" - ", mainPath.Select(x => x.Name)));
        }
    }

    private void Import()
    {
        string path;
        do
        {
            path = AnsiConsole.Ask<string>("Enter file path: ");
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found.");
                path = string.Empty;
            }
        } while(string.IsNullOrEmpty(path));

        using var reader = new StreamReader(File.OpenRead(path));

        string? line;
        do
        {
            line = reader.ReadLine();
        } while (line?.StartsWith("1") != true);

        AnsiConsole.Status()
            .Start("Importing", ctx => {
                while (!reader.EndOfStream)
                {
                    try
                    {
                        line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;
                        var fields = line.Split(DELIMITER);
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
                        if (!_people.Any(x => x.Id == person.Id)) _people.Add(person);
                    }catch(Exception ex) 
                    { 
                        Console.WriteLine($"ERROR: {ex.Message}");
                        Console.WriteLine($"Entry: {line}");
                    }
                }

                ctx.Status("Building relationship graph");
                ctx.Refresh();

                foreach (var person in _people)
                {
                    if (person.FatherName != null)
                    {
                        var father = _people.FirstOrDefault(x => x.Name == person.FatherName);
                        person.Father = father;
                    }
                    if (person.MotherName != null)
                    {
                        var mother = _people.FirstOrDefault(x => x.Name == person.MotherName);
                        person.Mother = mother;
                    }

                    if (!string.IsNullOrEmpty(person.HouseOwnerName) &&
                        (person.RelationshipWithHouseOwner?.ToLower() == "vợ"
                        || person.RelationshipWithHouseOwner?.ToLower() == "chồng"))
                    {
                        var spouse = _people.FirstOrDefault(x => x.Name == person.HouseOwnerName);
                        if (spouse == null) continue;
                        person.Spouse = spouse;
                        spouse.Spouse = person;
                    }
                }
            });

        
    }
}


class Person
{
    public string Town { get; set; }
    public string Name { get; set; }
    public string Id { get; set; }
    public bool IsHouseOwner { get; set; }
    public Person? Father { get; set; }
    public string? FatherName { get; set; }
    public Person? Mother { get; set; }
    public string? MotherName { get; set; }
    public Person? Spouse { get; set; }
    public string? RelationshipWithHouseOwner { get; set; }
    public string? HouseOwnerName { get; set; }
    public Gender Gender { get; set; }
    public string? CardNumber { get; set; }

}
enum Gender { Male, Female }
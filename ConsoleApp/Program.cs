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
        if(!path.EndsWith(".csv") && !path.EndsWith(".txt"))
        {
            Console.WriteLine("File must be of type CSV or TXT.");
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
                    person.Father = father;
                }
                if (person.MotherName != null)
                {
                    var mother = graph.FirstOrDefault(x => x.Name == person.MotherName);
                    person.Mother = mother;
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
class App
{
    public App(IEnumerable<Person> persons)
    {
        _people = persons.ToList();
    } 

    private readonly List<Person> _people;

    private bool _ended = false;

    public void Run()
    {
        while (!_ended)
        {
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .AddChoices(new[] {
                    "Lookup Relationships",
                    "Draw Family Tree",
                    "Quit"
                }));

            if (selection == "Quit") _ended = true;
            else if (selection == "Lookup Relationships")
            {
                string id1 = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter first person ID: ")
                .PromptStyle("green")
                .ValidationErrorMessage("ID not found")
                .Validate(id =>
                {
                    return _people.Any(x => x.Id == id);
                })
            );

                string id2 = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter second person ID: ")
                        .PromptStyle("green")
                        .ValidationErrorMessage("ID not found")
                        .Validate(id =>
                        {
                            return _people.Any(x => x.Id == id) && id != id1;
                        })
                    );
                LookupRelationship(id1, id2);
            }
            else if (selection == "Draw Family Tree")
            {
                string id = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter first person ID: ")
                    .PromptStyle("green")
                    .ValidationErrorMessage("ID not found")
                    .Validate(id =>
                    {
                        return _people.Any(x => x.Id == id);
                    })
                );
                DrawFamilyTree(id);
            }
        }
    }

    public void DrawFamilyTree(string id)
    {
        var startPerson = _people.First(x => x.Id == id);

        var root = new Tree($"Family tree of {startPerson.Name}");
        Queue<Person> unvisitedPeople = new();
        Dictionary<Person, Person?> rlts = new();
        Dictionary<Person, TreeNode> nodes = new();
        unvisitedPeople.Enqueue(startPerson);
        while (unvisitedPeople.Any())
        {
            var visited = unvisitedPeople.Dequeue();
            if (visited.Father != null && !rlts.ContainsKey(visited.Father))
            {
                rlts.Add(visited.Father, visited);
                unvisitedPeople.Enqueue(visited.Father); 
            }

            if (visited.Mother != null && !rlts.ContainsKey(visited.Mother))
            {
                rlts.Add(visited.Mother, visited);
                unvisitedPeople.Enqueue(visited.Mother);
            }
            if (visited.Spouse != null && !rlts.ContainsKey(visited.Spouse))
            {
                rlts.Add(visited.Spouse, visited);
                unvisitedPeople.Enqueue(visited.Spouse);
            }
            if (rlts.ContainsKey(visited) && rlts[visited] != null)
            {
                nodes[visited] = nodes[rlts[visited]!].AddNode($"{visited?.Name}-{visited?.Id}" ?? string.Empty);
            }
            else
            {
                nodes[visited] = root.AddNode($"{visited?.Name}-{visited?.Id}" ?? string.Empty);
            }
        }

        AnsiConsole.Write(root);
    }
    public List<Person> LookupRelationship(string id1, string id2)
    {
        var startNode = _people.First(x => x.Id == id1);
        var endNode = _people.First(x => x.Id == id2);

        Queue<Person> unvisitedNodes = new();
        Dictionary<Person, Person?> currentPath = new()
        {
            { startNode, null }
        };
        unvisitedNodes.Enqueue(startNode);

        Person? parentNode = null;

        List<Person> mainPath = new();
        while (unvisitedNodes.Any())
        {
            var visited = unvisitedNodes.Dequeue();

            if (visited == endNode) break;
            if (visited.Father != null && !currentPath.ContainsKey(visited.Father))
            {
                currentPath[visited.Father] = visited;
                unvisitedNodes.Enqueue(visited.Father);
            }
            if (visited.Mother != null && !currentPath.ContainsKey(visited.Mother))
            {
                currentPath[visited.Mother] = visited;
                unvisitedNodes.Enqueue(visited.Mother);
            }
            if (visited.Spouse != null && !currentPath.ContainsKey(visited.Spouse))
            {
                currentPath[visited.Spouse] = visited;
                unvisitedNodes.Enqueue(visited.Spouse);
            }

            parentNode = visited;
        }

        if (!currentPath.ContainsKey(endNode))
        {
            Console.WriteLine($"{startNode.Name} and {endNode.Name} are not related.");
        }
        else
        {
            Person index = endNode;
            while (index is not null && currentPath.ContainsKey(index))
            {
                mainPath.Add(index);
                index = currentPath[index]!;
            }

            mainPath.Reverse();
            foreach (var i in Enumerable.Range(0, mainPath.Count - 1))
            {
                PrintRelationship(mainPath[i], mainPath[i + 1]);
            }
        }
        return mainPath;
    }
    private static void PrintRelationship(Person person1, Person person2)
    {
        if (person2 == person1.Father || person2 == person1.Mother)
            Console.WriteLine($"{person1.Name}-{person1.Id} is the {(person1.Gender == Gender.Male ? "Son" : "Daughter")} of {person2.Name}-{person2.Id}");
        else if (person2 == person1.Spouse)
            Console.WriteLine($"{person1.Name}-{person1.Id} is the {(person1.Gender == Gender.Male ? "Husband" : "Wife")} of {person2.Name}-{person2.Id}");
        else if (person1 == person2.Father)
            Console.WriteLine($"{person1.Name}-{person1.Id} is the Father of {person2.Name}-{person2.Id}");
        else if (person1 == person2.Mother)
            Console.WriteLine($"{person1.Name}-{person1.Id} is the Mother of {person2.Name}-{person2.Id}");
        else
            Console.WriteLine($"{person1.Name}-{person1.Id} and {person2.Name}-{person2.Id} are not related");
    }
}


class Person
{
    public string? Town { get; set; }
    public string? Name { get; set; }
    public required string Id { get; set; }
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


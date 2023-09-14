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
    public List<Person> Children { get; set; } = new();

}


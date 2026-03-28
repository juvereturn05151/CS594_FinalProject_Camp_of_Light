using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerProfile
{
    public string Name;
    public int Age;
    public string Profession;
    public List<string> Interests = new();

    public PlayerProfile()
    {
        Name = string.Empty;
        Age = 0;
        Profession = string.Empty;
        Interests = new List<string>();
    }

    public PlayerProfile(string name, int age, string profession, List<string> interests)
    {
        Name = name;
        Age = age;
        Profession = profession;
        Interests = interests ?? new List<string>();
    }

    public override string ToString()
    {
        string interestText = Interests == null || Interests.Count == 0
            ? "None"
            : string.Join(", ", Interests);

        return $"Name: {Name}\nAge: {Age}\nProfession: {Profession}\nInterests: {interestText}";
    }
}
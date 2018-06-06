import { Component, OnInit } from '@angular/core';
import { Person } from '../models/person';
import { MOCK_PEOPLE } from '../models/mock-people';

@Component({
  selector: 'app-person',
  templateUrl: './person.component.html',
  styleUrls: ['./person.component.css']
})

export class PersonComponent implements OnInit {
  people: Person[] = MOCK_PEOPLE;
  selectedPerson: Person;

  constructor() { }

  ngOnInit() {
  }

  onSelect(person: Person): void {
    this.selectedPerson = person;
  }
}

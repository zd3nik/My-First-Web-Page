import { Component, OnInit } from '@angular/core';
import { Person } from '../models/person';
import { MOCK_PEOPLE } from '../models/mock-people';

@Component({
  selector: 'app-people',
  templateUrl: './people.component.html',
  styleUrls: ['./people.component.css']
})
export class PeopleComponent implements OnInit {
  people: Person[] = MOCK_PEOPLE;
  selectedPerson: Person;

  constructor() { }

  ngOnInit() {
  }

  onSelect(person: Person): void {
    this.selectedPerson = person;
  }
}

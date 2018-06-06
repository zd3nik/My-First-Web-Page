import { Component, OnInit } from '@angular/core';
import { Person } from '../models/person';
import { PeopleService } from '../people.service';

@Component({
  selector: 'app-people',
  templateUrl: './people.component.html',
  styleUrls: ['./people.component.css'],
  providers: [PeopleService],
})
export class PeopleComponent implements OnInit {
  people: Person[];
  selectedPerson: Person;

  constructor(private peopleService: PeopleService) { }

  ngOnInit() {
    this.getPeople();
  }

  getPeople(): void {
    this.peopleService.getPeople().subscribe(people => this.people = people);
  }

  onSelect(person: Person): void {
    this.selectedPerson = person;
  }
}

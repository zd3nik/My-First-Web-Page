import { Component, OnInit, EventEmitter } from '@angular/core';
import { Person } from '../models/person';
import { PeopleService } from '../services/people.service';

@Component({
  selector: 'app-people',
  templateUrl: './people.component.html',
  styleUrls: ['./people.component.css'],
  providers: [PeopleService],
})
export class PeopleComponent implements OnInit {
  nameFilter: string;
  people: Person[];
  selectedPerson: Person;

  constructor(private peopleService: PeopleService) { }

  ngOnInit() {
    this.getPeople();
  }

  getPeople(): void {
    const selectedId = this.selectedPerson ? this.selectedPerson.id : null;
    this.peopleService.getPeople(this.nameFilter).subscribe(people => {
      this.people = people;
      if (this.people) {
        this.selectedPerson = this.people.find(p => p.id === selectedId);
      } else {
        this.selectedPerson = null;
      }
    });
  }

  onSelect(person: Person): void {
    this.selectedPerson = person;
  }

  add(): void {
    this.selectedPerson = {}
  }

  delete(person: Person): void {
    const selectedId = this.selectedPerson ? this.selectedPerson.id : null;
    if (person && person.id) {
      this.peopleService.deletePerson(person.id).subscribe(_ => {
        this.peopleService.getPeople().subscribe(people => {
          this.people = people;
          this.selectedPerson = this.people.find(p => p.id === selectedId);
        });
      });
    }
  }

  updatePerson(person: Person): void {
    if (person != null) {
      const updatedEntry = this.people.find(p => p.id === person.id);
      if (updatedEntry) {
        const idx = this.people.indexOf(updatedEntry);
        this.people[idx] = person;
        this.selectedPerson = person;
      }
    }
  }

  addPerson(person: Person): void {
    if (person != null) {
      this.people.push(person);
      this.selectedPerson = person;
    }
  }
}

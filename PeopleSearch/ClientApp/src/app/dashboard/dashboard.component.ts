import { Component, OnInit } from '@angular/core';
import { Person } from '../models/person';
import { PeopleService } from '../services/people.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  providers: [PeopleService]
})
export class DashboardComponent implements OnInit {
  people: Person[] = [];

  constructor(private peopleService: PeopleService) { }

  ngOnInit() {
    this.getPeople();
  }

  getPeople(): void {
    this.peopleService.getPeople().subscribe(people => this.people = people);
  }

  avatarUri(person: Person): string {
    return person && person.avatarId && person.avatarId.trim().length > 0
      ? `url(api/image/${person.avatarId})`
      : 'url(api/image/profile_placeholder.png)';
  }
}

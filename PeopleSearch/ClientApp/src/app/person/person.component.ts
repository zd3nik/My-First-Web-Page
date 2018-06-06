import { Component, OnInit, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { Person } from '../models/person';
import { PeopleService } from '../people.service';

@Component({
  selector: 'app-person',
  templateUrl: './person.component.html',
  styleUrls: ['./person.component.css'],
  providers: [PeopleService],
})
export class PersonComponent implements OnInit {
  @Input() person: Person;

  constructor(
    private route: ActivatedRoute,
    private peopleService: PeopleService,
    private location: Location,
  ) { }

  ngOnInit() {
    this.getPerson();
  }

  getPerson(): void {
    this.person = null;
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.peopleService.getPerson(id).subscribe(person => this.person = person);
    }
  }
}

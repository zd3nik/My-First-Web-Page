import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { Person } from '../models/person';
import { PeopleService } from '../services/people.service';

@Component({
  selector: 'app-person',
  templateUrl: './person.component.html',
  styleUrls: ['./person.component.css'],
  providers: [PeopleService],
})
export class PersonComponent implements OnInit {
  @Input() person: Person;
  @Output() personUpdated = new EventEmitter<Person>(); 
  @Output() personAdded = new EventEmitter<Person>(); 

  constructor(
    private route: ActivatedRoute,
    private peopleService: PeopleService,
    private location: Location,
  ) { }

  ngOnInit() {
    this.getPerson();
  }

  getPerson(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.getPersonById(id);
  }

  getPersonById(id: string): void {
    this.person = null;
    if (id && id.trim().length > 0) {
      this.peopleService.getPerson(id).subscribe(person => this.person = person);
    }
  }

  save(): void {
    if (this.person) {
      const id = this.person.id;
      if (this.person.id) {
        this.peopleService.updatePerson(this.person).subscribe(_ => {
          this.personUpdated.emit(this.person);
        });
      } else {
        this.peopleService.addPerson(this.person).subscribe(p => {
          this.person = p;
          this.personAdded.emit(this.person);
        });
      }
    }
  }

  avatarUri(): string {
    return this.person && this.person.avatarUri && this.person.avatarUri.trim().length > 0
      ? `url(api/image/${this.person.avatarUri})`
      : 'url(api/image/profile_placeholder.png)';
  }
}

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
  private hasChanges: { [key: string]: boolean; } = {};
  private showEditor = true;

  constructor(
    private route: ActivatedRoute,
    private peopleService: PeopleService,
    private location: Location,
  ) { }

  ngOnInit() {
    this.getPerson();
  }

  personRemoved(id: string) {
    if (id) {
      delete this.hasChanges[id];
    }
  }

  getPerson(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.getPersonById(id);
  }

  getPersonById(id: string): void {
    this.person = null;
    if (id && id.trim().length > 0) {
      this.peopleService.getPerson(id).subscribe(person => {
        this.person = person;
        if (this.hasChanges[id]) {
          this.personUpdated.emit(this.person);
          delete this.hasChanges[id];
        }
      });
    }
  }

  onKey(event, up?, down?): void {
    switch (event.code) {
      case "Enter":
        event.target.blur();
        this.save();
        break;
      case "Escape":
        event.target.blur();
        break;
      case "ArrowUp":
        if (up) {
          up.focus();
        }
        break;
      case "ArrowDown":
        if (down) {
          down.focus();
        }
        break;
      default:
        // TODO only if text
        if (this.person) {
          this.hasChanges[this.person.id] = true;
        }
        break;
    }
  }

  changeAvatar(): void {
    this.showEditor = false;
  }

  uploadAvatarImage(event): void {
    const avatarImageFile: File = event.target.files[0];
    this.peopleService.uploadPersonAvatar(this.person.id, avatarImageFile)
      .subscribe(res => {
        console.log(res);
        this.showEditor = true;
      });
  }

  cancelChangeAvatar(): void {
    this.showEditor = true;
  }

  isChanged(): boolean {
    return this.person ? this.hasChanges[this.person.id] : false;
  }

  save(): void {
    if (this.person) {
      delete this.hasChanges[this.person.id];
      console.log("saving ", this.person);
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
  }

  reset(): void {
    if (this.person) {
      this.getPersonById(this.person.id);
    }
  }

  avatarUri(): string {
    return this.person && this.person.avatarId && this.person.avatarId.trim().length > 0
      ? `url(api/image/${this.person.avatarId})`
      : 'url(api/image/profile_placeholder.png)';
  }
}

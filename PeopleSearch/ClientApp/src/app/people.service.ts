import { Injectable } from '@angular/core';
import { Person } from './models/person';
import { MOCK_PEOPLE } from './models/mock-people';
import { Observable } from 'rxjs/Rx';
import { of } from 'rxjs/observable/of';

@Injectable()
export class PeopleService {

  constructor() { }

  getPeople(): Observable<Person[]> {
    return of(MOCK_PEOPLE);
  }

  getPerson(id: string): Observable<Person> {
    return of(MOCK_PEOPLE.find(person => person.id === id));
  }
}

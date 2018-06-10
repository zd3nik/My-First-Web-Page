import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/Rx';
import { of } from 'rxjs/observable/of';
import { catchError, tap } from 'rxjs/operators';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Person } from '../models/person';
import { AvatarImage } from '../models/avatar-image';

// TODO send messages to logging service instead of console

@Injectable()
export class PeopleService {
  private peopleUrl = 'api/people';
  private imageUrl = 'api/image';

  constructor(private httpClient: HttpClient) { }

  getPeople(nameFilter = ''): Observable<Person[]> {
    const params = new HttpParams().set('name', nameFilter);
    return this.httpGet<Person[]>('', [], params);
  }

  getPerson(id: string): Observable<Person> {
    return this.httpGet<Person>('/' + id, { id: id });
  }

  updatePerson(person: Person): Observable<any> {
    return this.httpPut('/' + person.id, person);
  }

  addPerson(person: Person): Observable<any> {
    return this.httpPost(person);
  }

  deletePerson(id: string): Observable<any> {
    return this.httpDelete('/' + id);
  }

  //uploadPersonAvatar(id: string, avatar: AvatarImage): Observable<any> {
  //  console.log(`begin httpPust: ${this.imageUrl}`, avatar);
    //}

  private httpGet<T>(path = '', defaultResult: T, params?: HttpParams): Observable<T> {
    const url = this.peopleUrl + path;
    console.log(`begin httpGet: ${url}`);
    return this.httpClient.get<T>(url, { params: params })
      .pipe(
        tap(d => console.log(`end httpGet: ${url}`, d)),
        catchError(this.handleError('httpGet', url, defaultResult))
      );
  }

  private httpPut<T>(path: string, data: T, defaultResult?: T): Observable<any> {
    const url = this.peopleUrl + path;
    console.log(`begin httpPut: ${url}`, data);
    return this.httpClient.put<T>(url, data)
      .pipe(
        tap(d => console.log(`end httpPut: ${url}`, d)),
        catchError(this.handleError('httpPut', url, defaultResult as T))
      );
  }

  private httpPost<T>(data: T, defaultResult?: T): Observable<any> {
    const url = this.peopleUrl;
    console.log(`begin httpPost: ${url}`, data);
    return this.httpClient.post<T>(url, data)
      .pipe(
        tap(d => console.log(`end httpPost: ${url}`, d)),
        catchError(this.handleError('httpPost', url, defaultResult as T))
      );
  }

  private httpDelete<T>(path: string, defaultResult?: T): Observable<any> {
    const url = this.peopleUrl + path;
    console.log(`begin httpDelete: ${url}`);
    return this.httpClient.delete(url)
      .pipe(
        tap(d => console.log(`end httpDelete: ${url}`, d)),
        catchError(this.handleError('httpDelete', url, defaultResult as T))
      );
  }

  private handleError<T>(methodName: String, url: string, result: T) {
    return (error: any): Observable<T> => {
      console.error(`error from ${methodName}() ${url}: `, error)
      return of(result);
    }
  }
}

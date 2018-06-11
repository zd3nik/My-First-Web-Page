import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/Rx';
import { of } from 'rxjs/observable/of';
import { catchError, tap } from 'rxjs/operators';
import { HttpClient, HttpParams, HttpHeaders } from '@angular/common/http';
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
    return this.httpGet<Person[]>(this.peopleUrl, [], params);
  }

  getPerson(id: string): Observable<Person> {
    return this.httpGet<Person>(this.peopleUrl + '/' + id, { id: id });
  }

  updatePerson(person: Person): Observable<any> {
    return this.httpPut(this.peopleUrl + '/' + person.id, person);
  }

  addPerson(person: Person): Observable<any> {
    return this.httpPost(this.peopleUrl, person);
  }

  deletePerson(id: string): Observable<any> {
    return this.httpDelete(this.peopleUrl + '/' + id);
  }

  uploadPersonAvatar(personId: string, avatarImageFile: File): Observable<any> {
    const fd = new FormData();
    fd.append("image", avatarImageFile, avatarImageFile.name);
    return this.httpPost(this.imageUrl + '/' + personId, fd);
  }

  private httpGet<T>(url: string, defaultResult: T, params?: HttpParams): Observable<T> {
    console.log(`begin httpGet: ${url}`);
    return this.httpClient.get<T>(url, { params: params })
      .pipe(
        tap(d => console.log(`end httpGet: ${url}`, d)),
        catchError(this.handleError('httpGet', url, defaultResult))
      );
  }

  private httpPut<T>(url: string, data: any, defaultResult?: T): Observable<any> {
    console.log(`begin httpPut: ${url}`, data);
    return this.httpClient.put<T>(url, data)
      .pipe(
        tap(d => console.log(`end httpPut: ${url}`, d)),
        catchError(this.handleError('httpPut', url, defaultResult as T))
      );
  }

  private httpPost<T>(url: string, data: any, defaultResult?: T): Observable<any> {
    console.log(`begin httpPost: ${url}`, data);
    return this.httpClient.post<T>(url, data)
      .pipe(
        tap(d => console.log(`end httpPost: ${url}`, d)),
        catchError(this.handleError('httpPost', url, defaultResult as T))
      );
  }

  private httpDelete<T>(url: string, defaultResult?: T): Observable<any> {
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

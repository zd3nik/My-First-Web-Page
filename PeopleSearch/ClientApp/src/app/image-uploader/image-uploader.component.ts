import { Component, OnInit, Input } from '@angular/core';
import { PeopleService } from '../services/people.service';

@Component({
  selector: 'app-image-uploader',
  templateUrl: './image-uploader.component.html',
  styleUrls: ['./image-uploader.component.css'],
  providers: [PeopleService],
})
export class ImageUploaderComponent implements OnInit {
  @Input() private personId: string;
  private selectedImageFile;

  constructor(private peopleService: PeopleService) { }

  ngOnInit() {
  }

  onFileSelected(event): void {
    const files = event.target.files;
    if (files && files.length > 0) {
      this.selectedImageFile = files[0];
    } else {
      this.selectedImageFile = null;
    }
  }

  onUpload(): void {
    if (this.personId && this.selectedImageFile) {
      //this.peopleService.uploadPersonAvatar(this.personId, this.selectedImageFile).subscribe();
    }
  }
}

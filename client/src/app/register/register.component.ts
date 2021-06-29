import { Component, Input, OnInit, Output, EventEmitter } from '@angular/core';
import { AccountService } from '../_services/account.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
})
export class RegisterComponent implements OnInit {
  @Output() cancelRegister = new EventEmitter();

  model: any = {};

  constructor(private accountService:AccountService) {}

  ngOnInit(): void {}

  Register() {
    this.accountService.Register(this.model).subscribe(
      (response) => {
        console.log(response);
        this.Cancel();
      },
      (error) => {
        console.log(error);
      },
      () => {}
    );
  }
  Cancel() {
    this.cancelRegister.emit(false);
  }
}

import { Component, Injectable, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { WeatherForecast, WeatherForecasts } from '../types/weatherForecast';

@Injectable()
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'weather';
  forecasts: WeatherForecasts = [];

  constructor(private http: HttpClient, private cdr: ChangeDetectorRef) { }

  ngOnInit(): void {
    this.http.get<WeatherForecasts>('api/weatherforecast').subscribe({
      next: result => {
        this.forecasts = result;
        this.cdr.detectChanges();
      },
      error: console.error,
    });
  }

  trackByDate(_index: number, forecast: WeatherForecast) {
    return forecast.date;
  }
}
import express from 'express';

const app = express();
const port = process.env["PORT"] || 3000;

const summaries = [
    'Freezing', 'Bracing', 'Chilly', 'Cool', 'Mild',
    'Warm', 'Balmy', 'Hot', 'Sweltering', 'Scorching'
];

app.get('/weatherforecast', (_, res) => {
    const forecasts: WeatherForecasts = Array.from({ length: 5 }, (_, index) => {
        const date = new Date();
        date.setDate(date.getDate() + index + 1);

        return {
            date: date.toISOString().split('T')[0],
            temperatureC: Math.floor(Math.random() * 75) - 20,
            summary: summaries[Math.floor(Math.random() * summaries.length)]
        };
    });

    res.json(forecasts);
});

app.listen(port, () => {
    console.log(`Server is running at http://localhost:${port}`);
});

interface WeatherForecast {
    date: string;
    temperatureC: number;
    summary: string;
}

type WeatherForecasts = WeatherForecast[];
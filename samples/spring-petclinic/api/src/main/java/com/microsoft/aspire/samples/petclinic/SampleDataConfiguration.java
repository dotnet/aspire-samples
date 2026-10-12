package com.microsoft.aspire.samples.petclinic;

import java.time.LocalDate;

import org.springframework.boot.CommandLineRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class SampleDataConfiguration {
    @Bean
    CommandLineRunner loadSampleData(OwnerRepository owners, VetRepository vets) {
        return args -> {
            if (owners.count() == 0) {
                var george = new Owner("George", "Franklin", "110 W. Liberty St.", "Madison", "6085551023");
                george.addPet(new Pet("Leo", "cat", LocalDate.of(2021, 9, 7)));

                var betty = new Owner("Betty", "Davis", "638 Cardinal Ave.", "Sun Prairie", "6085551749");
                betty.addPet(new Pet("Basil", "hamster", LocalDate.of(2022, 8, 6)));
                betty.addPet(new Pet("Rosy", "dog", LocalDate.of(2020, 4, 17)));

                var eduardo = new Owner("Eduardo", "Rodriquez", "1212 Park Ave.", "Madison", "6085552765");
                eduardo.addPet(new Pet("Jewel", "dog", LocalDate.of(2019, 3, 13)));

                owners.saveAll(java.util.List.of(george, betty, eduardo));
            }

            if (vets.count() == 0) {
                vets.saveAll(java.util.List.of(
                    new Vet("James", "Carter", "General practice"),
                    new Vet("Helen", "Leary", "Radiology"),
                    new Vet("Rafael", "Ortega", "Surgery")));
            }
        };
    }
}

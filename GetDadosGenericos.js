function getDados(opcao) {
    switch (opcao) {
        case (1): return GetDados1();
        case (2): return GetDados2();
        case (3): return GetDados3();
        case (4): return GetDados4();
        case (5): return GetDados5();
        case (6): return GetDados6();
        case (7): return GetDados7();
        case (8): return GetDados8();
        case (9): return GetDados9();
        case (10): return GetDados10();
        case (11): return GetDados11();
        case (12): return GetDados12();
        case (13): return GetDados13();
        case (14): return GetDados14();
        case (15): return GetDados15();
        case (16): return GetDados16();
        case (17): return GetDados17();
        case (18): return GetDados18();
        case (19): return GetDados19();
        case (20): return GetDados20();
        case (21): return GetDados21();
        case (22): return GetDados22();
        case (23): return GetDados23();
        case (24): return GetDados24();
        case (25): return GetDados25();
        case (26): return GetDados26();
        case (27): return GetDados27();
        case (28): return GetDados28();
        case (29): return GetDados29();
        case (30): return GetDados30();
        case (31): return GetDados31();
        case (32): return GetDados32();
        case (33): return GetDados33();
        default: return;
    }

}



function getDadosFiltrados(opcao, filtro) {
    var dados = getDados(opcao);
    var soma = 0.00;
    var dadosFiltrados = dados.filter((dado) => {
        if (filtro.Clima && filtro.DiaSemana && filtro.Hora) {
            if (dado.dia_semana == filtro.DiaSemana && dado.clima == filtro.Clima && dado.hora == filtro.Hora) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.Clima && filtro.DiaSemana && filtro.Periodo) {
            if (dado.dia_semana == filtro.DiaSemana && dado.clima == filtro.Clima && dado.periodo == filtro.Periodo) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.Clima && filtro.DiaSemana && filtro.Turno) {
            if (dado.dia_semana == filtro.DiaSemana && dado.clima == filtro.Clima && dado.turno == filtro.Turno) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.Clima && filtro.DiaSemana) {
            if (dado.dia_semana == filtro.DiaSemana && dado.clima == filtro.Clima) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.Clima && filtro.Hora) {
            if (dado.clima == filtro.Clima && dado.hora == filtro.Hora) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.DiaSemana && filtro.Hora) {
            if (dado.dia_semana == filtro.DiaSemana && dado.hora == filtro.Hora) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.Clima && filtro.Periodo) {
            if (dado.clima == filtro.Clima && dado.periodo == filtro.Periodo) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.DiaSemana && filtro.Periodo) {
            if (dado.dia_semana == filtro.DiaSemana && dado.periodo == filtro.Periodo) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.Clima && filtro.Turno) {
            if (dado.clima == filtro.Clima && dado.turno == filtro.Turno) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.DiaSemana && filtro.Turno) {
            if (dado.dia_semana == filtro.DiaSemana && dado.turno == filtro.Turno) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.Clima) {
            if (dado.clima == filtro.Clima) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.DiaSemana) {
            if (dado.dia_semana == filtro.DiaSemana) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.Hora) {
            if (dado.hora == filtro.Hora) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.Periodo) {
            if (dado.periodo == filtro.Periodo) {
                soma += dado.proporcao;
                return true;
            }
        } else if (filtro.Turno) {
            if (dado.turno == filtro.Turno) {
                soma += dado.proporcao;
                return true;
            }
        } else {
            soma += dado.proporcao;
            return true;
        }
        return false;
    })
    if (soma == 0.00 || dadosFiltrados.length == 0)
        return 0

    return soma / dadosFiltrados.length;
}
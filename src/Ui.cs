using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Windows.Forms;

namespace RealFat32
{
    internal static class Ui
    {
        public static string Language = "es";
        public static bool Night;
        public static Color Window { get { return Night ? Brand.Background : Color.FromArgb(247, 247, 248); } }
        public static Color Surface { get { return Night ? Brand.Surface : SystemColors.Window; } }
        public static Color Text { get { return Night ? Color.White : SystemColors.ControlText; } }
        public static Color Muted { get { return Night ? Brand.Muted : SystemColors.GrayText; } }
        public static Color Green { get { return Night ? Brand.Accent : Color.FromArgb(20, 128, 69); } }
        public static Color Warning { get { return Night ? Color.FromArgb(255, 191, 132) : Color.FromArgb(145, 65, 12); } }
        private static readonly Dictionary<string, string[]> strings = new Dictionary<string, string[]>();
        static Ui()
        {
            Add("Automático (idioma de Windows)", "Automatic (Windows language)", "Automático (idioma do Windows)");
            Add("Canal de actualizaciones", "Update channel", "Canal de atualizações");
            Add("Estable", "Stable", "Estável"); Add("Beta", "Beta", "Beta"); Add("Alpha", "Alpha", "Alpha");
            Add("Buscar actualizaciones", "Check for updates", "Buscar atualizações");
            Add("Acerca de", "About", "Sobre"); Add("Historial de cambios", "Changelog", "Histórico de alterações");
            Add("Creada por Fabian Gonzalez Dev", "Created by Fabian Gonzalez Dev", "Criado por Fabian Gonzalez Dev");
            Add("Empresa: PoligonTek", "Company: PoligonTek", "Empresa: PoligonTek");
            Add("Buscando actualizaciones…", "Checking for updates…", "Buscando atualizações…");
            Add("No hay actualizaciones disponibles.", "No updates are available.", "Não há atualizações disponíveis.");
            Add("No se pudo comprobar: ", "Could not check: ", "Não foi possível verificar: ");
            Add("El repositorio aún no está disponible públicamente.", "The repository is not publicly available yet.", "O repositório ainda não está disponível publicamente.");
            Add("Hay una nueva actualización. ¿Actualizar?", "A new update is available. Update now?", "Há uma nova atualização. Atualizar agora?");
            Add("Actualización de RealFat32", "RealFat32 update", "Atualização do RealFat32");
            Add("Sí", "Yes", "Sim"); Add("No", "No", "Não");
            Add("Descargando y verificando la actualización…", "Downloading and verifying the update…", "Baixando e verificando a atualização…");
            Add("Se cerrará y reiniciará la aplicación.", "The application will close and restart.", "O aplicativo será fechado e reiniciado.");
            Add("No se pudo actualizar: ", "Could not update: ", "Não foi possível atualizar: ");
            Add("Actualización disponible: ", "Update available: ", "Atualização disponível: ");
            Add("La verificación SHA256 de la actualización falló.", "Update SHA256 verification failed.", "A verificação SHA256 da atualização falhou.");
            Add("La versión descargada no coincide con la publicación.", "The downloaded version does not match the release.", "A versão baixada não corresponde à publicação.");
            Add("El ejecutable debe llamarse RealFat32.exe para actualizarse.", "The executable must be named RealFat32.exe to update itself.", "O executável deve se chamar RealFat32.exe para ser atualizado.");
            Add("Configuración", "Settings", "Configurações");
            Add("Apariencia", "Appearance", "Aparência");
            Add("Windows (claro)", "Windows (light)", "Windows (claro)");
            Add("Nocturno", "Night", "Noturno");
            Add("Idioma", "Language", "Idioma");
            Add("Guardar", "Save", "Salvar"); Add("Cancelar", "Cancel", "Cancelar"); Add("Cerrar", "Close", "Fechar");
            Add("No se pudo guardar la configuración: ", "Could not save settings: ", "Não foi possível salvar as configurações: ");
            Add("Tus unidades. FAT32. Sin vueltas.", "Your drives. FAT32. Made simple.", "Suas unidades. FAT32. Sem complicações.");
            Add("01   ELIGE UNA UNIDAD", "01   CHOOSE A DRIVE", "01   ESCOLHA UMA UNIDADE");
            Add("↻  Actualizar", "↻  Refresh", "↻  Atualizar");
            Add("Unidad de destino", "Target drive", "Unidade de destino");
            Add("02   ETIQUETA DEL VOLUMEN", "02   VOLUME LABEL", "02   RÓTULO DO VOLUME");
            Add("TAMAÑO DE CLÚSTER", "CLUSTER SIZE", "TAMANHO DO CLUSTER");
            Add("Etiqueta del volumen, máximo 11 caracteres", "Volume label, up to 11 characters", "Rótulo do volume, até 11 caracteres");
            Add("Automático (recomendado)", "Automatic (recommended)", "Automático (recomendado)");
            Add("Tamaño de clúster", "Cluster size", "Tamanho do cluster");
            Add("Formato rápido · FAT32 admite archivos de hasta 4 GiB menos 1 byte.", "Quick format · FAT32 supports files up to 4 GiB minus 1 byte.", "Formatação rápida · FAT32 aceita arquivos de até 4 GiB menos 1 byte.");
            Add("Se eliminarán todos los archivos del volumen.\nEl formato rápido no es un borrado seguro.", "All files on the volume will be deleted.\nQuick format does not securely erase data.", "Todos os arquivos do volume serão excluídos.\nA formatação rápida não é uma exclusão segura.");
            Add("Formatear en FAT32", "Format as FAT32", "Formatar em FAT32");
            Add("Continuar como administrador", "Continue as administrator", "Continuar como administrador");
            Add("Abrir como administrador", "Run as administrator", "Abrir como administrador");
            Add("No se concedieron permisos de administrador.", "Administrator permission was not granted.", "A permissão de administrador não foi concedida.");
            Add("Listo para actualizar las unidades.", "Ready to refresh drives.", "Pronto para atualizar as unidades.");
            Add("Conecta una unidad y pulsa Actualizar.", "Connect a drive and click Refresh.", "Conecte uma unidade e clique em Atualizar.");
            Add("Selecciona el volumen que quieres preparar.", "Select the volume you want to prepare.", "Selecione o volume que deseja preparar.");
            Add("Selecciona una unidad…", "Select a drive…", "Selecione uma unidade…");
            Add("Disco", "Disk", "Disco"); Add("Capacidad:", "Capacity:", "Capacidade:"); Add("Libre:", "Free:", "Livre:");
            Add("Formato actual:", "Current format:", "Formato atual:"); Add("Sector:", "Sector:", "Setor:");
            Add("Compatible con FAT32", "FAT32 compatible", "Compatível com FAT32");
            Add("Sin etiqueta", "No label", "Sem rótulo");
            Add("Vista de demostración · no realiza operaciones de disco.", "Demo mode · no disk operations are performed.", "Modo de demonstração · nenhuma operação de disco é realizada.");
            Add("Ver celebración", "Preview celebration", "Ver comemoração");
            Add("Consultando unidades y protecciones de Windows…", "Checking drives and Windows protections…", "Consultando unidades e proteções do Windows…");
            Add(" volumen(es) detectado(s). Selecciona una unidad.", " volume(s) found. Select a drive.", " volume(s) encontrado(s). Selecione uma unidade.");
            Add("No se pudieron consultar las unidades: ", "Could not query drives: ", "Não foi possível consultar as unidades: ");
            Add("Revisa las opciones", "Check the options", "Verifique as opções");
            Add("No se completó el formato", "Format did not complete", "A formatação não foi concluída");
            Add("Formato finalizado en ", "Format completed on ", "Formatação concluída em ");
            Add(" · FAT32 listo.", " · FAT32 is ready.", " · FAT32 está pronto.");
            Add("Confirmar formato de ", "Confirm format of ", "Confirmar formatação de ");
            Add("Se eliminarán TODOS los archivos de ", "ALL files will be deleted from ", "TODOS os arquivos serão excluídos de ");
            Add("Etiqueta actual: ", "Current label: ", "Rótulo atual: ");
            Add("Comprueba la unidad. El botón se habilitará después de 3 segundos.", "Check the drive. The button will be enabled after 3 seconds.", "Confira a unidade. O botão será habilitado após 3 segundos.");
            Add("Formatear", "Format", "Formatar");
            Add("Disco totalmente formateado", "Drive fully formatted", "Disco totalmente formatado");
            Add("Formato rápido FAT32 completado. La unidad está lista.", "FAT32 quick format complete. Your drive is ready.", "Formatação rápida FAT32 concluída. A unidade está pronta.");
            Add("Celebración de demostración · no se formateó ninguna unidad.", "Demo celebration · no drive was formatted.", "Comemoração de demonstração · nenhuma unidade foi formatada.");
            Add("No se pudo identificar el volumen.", "Could not identify the volume.", "Não foi possível identificar o volume.");
            Add("RAW / no disponible", "RAW / unavailable", "RAW / indisponível"); Add("Sin acceso", "Access denied", "Sem acesso");
            Add("Abre como administrador para comprobar el cifrado.", "Run as administrator to check encryption.", "Abra como administrador para verificar a criptografia.");
            Add("Volumen cifrado con BitLocker. Descífralo primero.", "BitLocker-encrypted volume. Decrypt it first.", "Volume criptografado com BitLocker. Descriptografe-o primeiro.");
            Add("Unidad de solo lectura.", "Read-only drive.", "Unidade somente leitura."); Add("Unidad fuera de línea.", "Drive is offline.", "Unidade offline.");
            Add("Tipo de partición MBR reservado o no compatible.", "Reserved or unsupported MBR partition type.", "Tipo de partição MBR reservado ou incompatível.");
            Add("Solo se admiten particiones GPT de datos básicos.", "Only GPT basic data partitions are supported.", "Somente partições GPT de dados básicos são aceitas.");
            Add("Tabla de particiones no compatible.", "Unsupported partition table.", "Tabela de partições incompatível.");
            Add("El disco no proporciona una identidad estable.", "The disk does not provide a stable identity.", "O disco não fornece uma identidade estável.");
            Add("Disco protegido: Windows, arranque, paginación o RealFat32.", "Protected disk: Windows, boot, paging or RealFat32.", "Disco protegido: Windows, inicialização, paginação ou RealFat32.");
            Add("Se requieren permisos de administrador.", "Administrator permission is required.", "É necessária a permissão de administrador.");
            Add("La unidad cambió. Actualiza y vuelve a seleccionarla.", "The drive changed. Refresh and select it again.", "A unidade mudou. Atualize e selecione-a novamente.");
            Add("El inicio de la partición no se puede representar en FAT32.", "The partition start cannot be represented in FAT32.", "O início da partição não pode ser representado em FAT32.");
            Add("La identidad o los límites del volumen no coinciden. No se escribió nada.", "Volume identity or boundaries do not match. Nothing was written.", "A identidade ou os limites do volume não coincidem. Nada foi gravado.");
            Add("La capacidad del volumen cambió.", "The volume capacity changed.", "A capacidade do volume mudou."); Add("El tamaño de sector cambió.", "The sector size changed.", "O tamanho do setor mudou.");
            Add("Volumen bloqueado. Iniciando formato rápido…", "Volume locked. Starting quick format…", "Volume bloqueado. Iniciando formatação rápida…");
            Add("El formato se interrumpió. El volumen puede haber quedado sin formato. ", "Formatting was interrupted. The volume may be unformatted. ", "A formatação foi interrompida. O volume pode ter ficado sem formatação. ");
            Add("No se inició el formato. ", "Formatting did not start. ", "A formatação não foi iniciada. ");
            Add("Formato finalizado. FAT32 está listo.", "Format complete. FAT32 is ready.", "Formatação concluída. FAT32 está pronto.");
            Add("No se pudo actualizar el tipo MBR a FAT32 LBA.", "Could not update the MBR type to FAT32 LBA.", "Não foi possível atualizar o tipo MBR para FAT32 LBA.");
            Add("La unidad está en uso. Cierra sus archivos, ventanas y aplicaciones y vuelve a intentar.", "The drive is in use. Close its files, windows and applications and try again.", "A unidade está em uso. Feche seus arquivos, janelas e aplicativos e tente novamente.");
            Add("Windows rechazó la operación del volumen.", "Windows rejected the volume operation.", "O Windows rejeitou a operação do volume.");
            Add("Windows devolvió información incompleta del disco.", "Windows returned incomplete disk information.", "O Windows retornou informações incompletas do disco.");
            Add("Solo se admiten sectores lógicos de 512 o 4096 bytes.", "Only 512-byte or 4096-byte logical sectors are supported.", "Somente setores lógicos de 512 ou 4096 bytes são aceitos.");
            Add("El volumen debe medir entre 512 MiB y 2 TiB y estar alineado al sector.", "The volume must be between 512 MiB and 2 TiB and sector-aligned.", "O volume deve ter entre 512 MiB e 2 TiB e estar alinhado ao setor.");
            Add("Tamaño de clúster inválido (máximo 32 KiB).", "Invalid cluster size (maximum 32 KiB).", "Tamanho de cluster inválido (máximo 32 KiB).");
            Add("La cantidad de sectores excede FAT32.", "The sector count exceeds FAT32 limits.", "A quantidade de setores excede o limite do FAT32.");
            Add("Ese tamaño de clúster no produce un volumen FAT32 válido. Usa Automático.", "That cluster size does not produce a valid FAT32 volume. Use Automatic.", "Esse tamanho de cluster não produz um volume FAT32 válido. Use Automático.");
            Add("No se pudo obtener una geometría FAT32 compatible.", "Could not determine a compatible FAT32 layout.", "Não foi possível obter uma geometria FAT32 compatível.");
            Add("La etiqueta admite hasta 11 caracteres.", "The label supports up to 11 characters.", "O rótulo aceita até 11 caracteres.");
            Add("Usa letras sin acentos, números, espacios o guiones para la etiqueta.", "Use unaccented letters, numbers, spaces or hyphens for the label.", "Use letras sem acentos, números, espaços ou hífens no rótulo.");
            Add("Inicializando tablas FAT…", "Initializing FAT tables…", "Inicializando tabelas FAT…"); Add("Guardando sector de arranque…", "Saving boot sector…", "Salvando setor de inicialização…");
        }
        private static void Add(string es, string en, string pt) { strings.Add(es, new[] { es, en, pt }); }
        public static string T(string s) { string[] row; return strings.TryGetValue(s, out row) ? row[Language == "en" ? 1 : Language == "pt" ? 2 : 0] : s; }
        public static string Error(string s)
        {
            if (Language == "es" || s == null) return s;
            foreach (var key in strings.Keys.OrderByDescending(k => k.Length)) s = s.Replace(key, T(key));
            return s;
        }
        public static string Item(object item)
        {
            var v = item as Volume;
            return v == null ? T(item.ToString()) : v.Letter + "   " + (String.IsNullOrEmpty(v.Label) ? T("Sin etiqueta") : v.Label) + "   ·   " + Volume.Human(v.Size);
        }
        public static void Capture(Control c)
        {
            if (strings.ContainsKey(c.Text)) c.Tag = c.Text;
            foreach (Control child in c.Controls) Capture(child);
        }
        public static void Apply(Control c)
        {
            if (c.Tag is string) c.Text = T((string)c.Tag);
            c.BackColor = c is TextBox || c is ComboBox || c.Name == "card" ? Surface : Window;
            c.ForeColor = c.Name == "warning" ? Warning : Text;
            var b = c as Button;
            if (b != null)
            {
                b.UseVisualStyleBackColor = !Night;
                b.FlatStyle = Night ? FlatStyle.Flat : FlatStyle.Standard;
                if (Night) { b.BackColor = b.Name == "primary" ? Brand.Accent : Surface; b.ForeColor = b.Name == "primary" ? Brand.Background : Text; }
                if (b.Name == "danger") { b.UseVisualStyleBackColor = false; b.BackColor = Color.FromArgb(179, 48, 61); b.ForeColor = Color.White; }
            }
            foreach (Control child in c.Controls) Apply(child);
        }
    }

    internal static class Preferences
    {
        public static string LanguageChoice = "auto";
        public static string UpdateChannel = "stable";
        public static string DetectLanguage(CultureInfo culture)
        {
            string code = culture.TwoLetterISOLanguageName;
            return code == "es" || code == "pt" ? code : "en";
        }
        public static string DefaultPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RealFat32", "settings.txt"); } }
        public static void Load(string path, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentUICulture;
            LanguageChoice = "auto"; UpdateChannel = "stable"; Ui.Language = DetectLanguage(culture); Ui.Night = false;
            try
            {
                if (!File.Exists(path)) return;
                var lines = File.ReadAllLines(path);
                if (lines.Length > 0 && new[] { "auto", "es", "en", "pt" }.Contains(lines[0])) LanguageChoice = lines[0];
                if (LanguageChoice != "auto") Ui.Language = LanguageChoice;
                Ui.Night = lines.Length > 1 && lines[1] == "night";
                if (lines.Length > 2 && new[] { "stable", "beta", "alpha" }.Contains(lines[2])) UpdateChannel = lines[2];
            }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        public static void Save(string path, string language, bool night, string channel = null)
        {
            channel = channel ?? UpdateChannel;
            if (!new[] { "auto", "es", "en", "pt" }.Contains(language) || !new[] { "stable", "beta", "alpha" }.Contains(channel)) throw new ArgumentException("Invalid preference");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllLines(path, new[] { language, night ? "night" : "light", channel });
            LanguageChoice = language; UpdateChannel = channel;
            Ui.Language = language == "auto" ? DetectLanguage(CultureInfo.CurrentUICulture) : language; Ui.Night = night;
        }
    }

    internal sealed class SettingsForm : Form
    {
        public SettingsForm()
        {
            Text = Ui.T("Configuración"); ClientSize = new Size(450, 345); Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
            var language = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(24, 53), Size = new Size(400, 30) };
            language.Items.AddRange(new object[] { Ui.T("Automático (idioma de Windows)"), "Español", "English", "Português" }); language.SelectedIndex = Preferences.LanguageChoice == "auto" ? 0 : Preferences.LanguageChoice == "es" ? 1 : Preferences.LanguageChoice == "en" ? 2 : 3;
            var theme = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(24, 132), Size = new Size(400, 30) };
            theme.Items.AddRange(new object[] { Ui.T("Windows (claro)"), Ui.T("Nocturno") }); theme.SelectedIndex = Ui.Night ? 1 : 0;
            var channel = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(24, 211), Size = new Size(400, 30) };
            channel.Items.AddRange(new object[] { Ui.T("Estable"), "Beta", "Alpha" }); channel.SelectedIndex = Preferences.UpdateChannel == "alpha" ? 2 : Preferences.UpdateChannel == "beta" ? 1 : 0;
            var save = new Button { Text = Ui.T("Guardar"), Location = new Point(264, 285), Size = new Size(160, 36) };
            var cancel = new Button { Text = Ui.T("Cancelar"), Location = new Point(24, 285), Size = new Size(140, 36), DialogResult = DialogResult.Cancel };
            save.Click += delegate
            {
                try { Preferences.Save(Preferences.DefaultPath, new[] { "auto", "es", "en", "pt" }[language.SelectedIndex], theme.SelectedIndex == 1, new[] { "stable", "beta", "alpha" }[channel.SelectedIndex]); DialogResult = DialogResult.OK; Close(); }
                catch (Exception ex) { MessageBox.Show(this, Ui.T("No se pudo guardar la configuración: ") + ex.Message, Ui.T("Configuración"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            Controls.AddRange(new Control[] { new Label { Text = Ui.T("Idioma"), Location = new Point(24, 22), AutoSize = true }, language,
                new Label { Text = Ui.T("Apariencia"), Location = new Point(24, 101), AutoSize = true }, theme,
                new Label { Text = Ui.T("Canal de actualizaciones"), Location = new Point(24, 180), AutoSize = true }, channel, save, cancel });
            CancelButton = cancel; AcceptButton = save; Ui.Apply(this);
        }
    }
}

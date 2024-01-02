using BackupHelper.Core.DataTransfer;
using BackupHelper.Core.Features;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BackupHelper.Api.Controllers
{
    [Route("api/backup")]
    [ApiController]
    public class BackupController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BackupController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBackup([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] BackupDto backupDto)
        {
            await _mediator.Send(new CreateBackupCommand(backupDto.BackupConfiguration, backupDto.BackupFilePath));
            return Ok();
        }

        [HttpPost]
        [Route("configuration")]
        public async Task<IActionResult> SaveBackupConfiguration([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] SaveBackupConfigDto configurationSaveDto)
        {
            await _mediator.Send(new SaveBackupConfigurationCommand(configurationSaveDto.BackupConfiguration, configurationSaveDto.ConfigurationSavePath));
            return Ok();
        }
    }
}
